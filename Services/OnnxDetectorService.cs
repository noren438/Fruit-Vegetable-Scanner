using Frugt_Grønt_Scanner.Models;
using Microsoft.ML.OnnxRuntime;
using Microsoft.ML.OnnxRuntime.Tensors;
using SkiaSharp;
using System.Diagnostics;
using System.Text.Json;
using System.Text.RegularExpressions;

namespace Frugt_Grønt_Scanner.Services
{
    public class OnnxDetectorService
    {
        private const int ImageSize = 640;
        private const float MinimumConfidence = 0.25f;

        private readonly ProduktService produktListe;
        private readonly OnnxModelProviderService providerService;

        private readonly string[] standardClassNames =
        {
                 "apple", "banana", "broccoli", "cabbage", "carrot", "cauliflower",
                 "cherry", "cucumber", "dragon fruit", "eggplant", "garlic", "grapes",
                 "green chili", "green grapes", "lemon", "lychee", "mango", "orange",
                 "peanut", "pear", "pineapple", "pomelo", "potato", "radish",
                 "red chili", "strawberry", "tomato", "watermelon",
                 "bell pepper", "bitter melon", "calabash", "ginger", "guava",
                 "lady finger", "onion", "pumpkin", "sugar apple"

        };
       

        public OnnxDetectorService(ProduktService produkter, OnnxModelProviderService provider)
        {
            produktListe = produkter;
            providerService = provider;
        }
        public async Task<DetectionResults> DetectAsync(Stream imageStream)
        {
            var session = await providerService.GetSessionAsync();
            var inputNavn = session.InputMetadata.Keys.First();

            using var sourceBitmap = SKBitmap.Decode(imageStream);
            if (sourceBitmap == null)
            {
                return new DetectionResults
                {
                    Success = false,
                    ErrorMessage = "Filen kunne ikke læses som et billede"
                };
            }
            var classNames = GetClassNames(session);
            var prediction = PredictMultipleResults(session, inputNavn, sourceBitmap, classNames);

            return new DetectionResults
            {
                Success = prediction.Any(),
                DetectedItems = prediction,
                ProduktNavn = prediction.FirstOrDefault()?.Navn?? "Ukendt produkt",
                ProduktKode = prediction.FirstOrDefault()?.Kode ?? "0000",
                Type = prediction.FirstOrDefault()?.Type ?? "Ukendt",
                Confidence = prediction.FirstOrDefault()?.Confidence ?? 0f,
                ErrorMessage = prediction.Any()? null: "Ingen produkter blev fundet"
            };
        }
        private List<DetectedItem> PredictMultipleResults(InferenceSession session, string inputNavn, SKBitmap sourceBitmap, string[] classNames)
        {
            var allResults = new List<DetectedItem>();
            using var resizedBitmap = LetterBoxResize(sourceBitmap, ImageSize, ImageSize);

            var primaryMode = new InputMode(NormalizeTo01: true, UseBgr: false);
            allResults.AddRange(RunInference(session, inputNavn, resizedBitmap, primaryMode, classNames));

            if (!allResults.Any())
            {
                var fallbackModes = new[]
                {
                    new InputMode(NormalizeTo01: true, UseBgr: true),
                    new InputMode(NormalizeTo01: false, UseBgr: false)
                };

                foreach (var mode in fallbackModes)
                {
                    allResults.AddRange(RunInference(session, inputNavn, resizedBitmap, mode, classNames));

                    if (allResults.Any())
                        break;
                }
            }

            return allResults
                .GroupBy(x => x.Navn)
                .Select(n => n.OrderByDescending(x => x.Confidence).First())
                .OrderByDescending(x => x.Confidence)
                .Take(10)
                .ToList();
        }

        private List<DetectedItem> RunInference(InferenceSession session, string inputNavn, SKBitmap bitmap, InputMode inputMode, string[] classNames)
        {
            var tensor = CreateTensorFromBitmap(bitmap, inputMode.NormalizeTo01, inputMode.UseBgr);

            using var results = session.Run(new[]
            {
                NamedOnnxValue.CreateFromTensor(inputNavn, tensor)
            });

            var firstTensorOutput = results.FirstOrDefault();
            if (firstTensorOutput == null)
                return new List<DetectedItem>();

            Tensor<float> output;
            try
            {
                output = firstTensorOutput.AsTensor<float>();
            }
            catch
            {
                return new List<DetectedItem>();
            }

            return GetMultiplePrediction(output, classNames);
        }
       
        private static DenseTensor<float> CreateTensorFromBitmap(SKBitmap bitmap, bool normalizeTo01, bool useBgr)
        {
            var tensor = new DenseTensor<float>(new[] { 1, 3, ImageSize, ImageSize });

            for (var y = 0; y < ImageSize; y++)
            {
                for (var x = 0; x < ImageSize; x++)
                {
                    var pixel = bitmap.GetPixel(x, y);
                    var r = (float)pixel.Red;
                    var g = (float)pixel.Green;
                    var b = (float)pixel.Blue;

                    if (normalizeTo01)
                    {
                        r /= 255f;
                        g /= 255f;
                        b /= 255f;

                    }
                    tensor[0, 0, y, x] = useBgr ? b : r;
                    tensor[0, 1, y, x] = g;
                    tensor[0, 2, y, x] = useBgr ? r : b;
                }
            }
            return tensor;
        }
        private static SKBitmap LetterBoxResize(SKBitmap source, int targetWidth, int targetHeight)
        {
            var output = new SKBitmap(targetWidth, targetHeight, source.ColorType, source.AlphaType);

            using var canvas = new SKCanvas(output);
            canvas.Clear(new SKColor(114, 114, 114));

            var scale = Math.Min((float)targetWidth / source.Width,
                                (float)targetHeight / source.Height);

            var resizedWidth = Math.Max(1, (int)Math.Round(source.Width * scale));
            var resizedHeigth = Math.Max(1, (int)Math.Round(source.Height * scale));

            using var resized = source.Resize(
                new SKImageInfo(resizedWidth, resizedHeigth), SKFilterQuality.High);

            if (resized == null)
            
                return output;

                var x = (targetWidth - resizedWidth) / 2;
                var y = (targetHeight - resizedHeigth) / 2;

                canvas.DrawBitmap(resized, x, y);
            
                return output;
        }

        private List<DetectedItem> GetMultiplePrediction(Tensor<float> output, string[] classNames)
        {
            var dimensions = output.Dimensions.ToArray();

            var boxes = new List<YoloBox>();

            if (dimensions.Length < 2)
                return new List<DetectedItem>();

            bool hasBatch = dimensions.Length == 3;
            bool channelsFirst = false;

            int channels;
            int boxCount;

            if (hasBatch)
            {
                var option1Channels = dimensions[1];
                var option1Boxes = dimensions[2];
                var option2Boxes = dimensions[1];
                var option2Channels = dimensions[2];

                var expectedClassCount = classNames.Length;
                var minChannels = expectedClassCount + 4;
                var option1Valid = option1Channels >= minChannels && option1Boxes > 1;
                var option2Valid = option2Channels >= minChannels && option2Boxes > 1;

                if (!option1Valid && !option2Valid)
                    return new List<DetectedItem>();

                channelsFirst = option1Valid && (!option2Valid || option1Channels <= option2Channels);

                if (channelsFirst)
                {
                    channels = option1Channels;
                    boxCount = option1Boxes;
                }
                else
                {
                    channels = option2Channels;
                    boxCount = option2Boxes;
                }
            }
            else
            {
                boxCount = dimensions[0];
                channels = dimensions[1];
            }

            if (channels < 5)
            {
                return new List<DetectedItem>();
            }

            var hasObject = channels >= classNames.Length + 5;
            var classStart = hasObject ? 5 : 4;
            var classCount = classNames.Length;

            if (classCount <= 0 || channels < classStart + classCount)
                return new List<DetectedItem>();


            for (var box = 0; box < boxCount; box++)
            {
                float GetValue (int channel)
                {
                    return channelsFirst ?
                    output[0, channel, box] :
                    hasBatch ?
                    output[0, box, channel] :
                    output[box, channel];
                }
                var centerX = GetValue(0);
                var centerY = GetValue(1);
                var width = GetValue(2);
                var height = GetValue(3);

                if (width <= 0f || height <= 0f)
                    continue;

                var objectness = 1f;

                if (hasObject)
                    objectness = NormalizeScore(GetValue(4));

                var bestClassIndex = -1;
                var bestConfidence = 0f;

                var rawClassScores = new float[classCount];
                var classScoresNeedSoftmax = false;

                for (var classIndex = 0; classIndex < classCount; classIndex++)
                {
                    var rawScore = GetValue(classStart + classIndex);
                    rawClassScores[classIndex] = rawScore;
                    if (rawScore < 0f || rawScore > 1f)
                        classScoresNeedSoftmax = true;
                }

                float[] classProbabilities;
                if (classScoresNeedSoftmax)
                {
                    classProbabilities = Softmax(rawClassScores);
                }
                else
                {
                    classProbabilities = rawClassScores;
                }

                for (var classIndex = 0; classIndex < classCount; classIndex++)
                {
                    var classScore = classProbabilities[classIndex];
                    var confidence = classScore * objectness;

                    if (confidence > bestConfidence)
                    {
                        bestConfidence = confidence;
                        bestClassIndex = classIndex;
                    }
                }
                if (bestClassIndex < 0 || bestConfidence < MinimumConfidence)
                    continue;

                boxes.Add(new YoloBox
                {
                    ClassIndex = bestClassIndex,
                    Confidence = bestConfidence,
                    X = centerX - width / 2f,
                    Y = centerY - height / 2f,
                    Width = width,
                    Height = height
                });
            }

            var nmsBoxes = ApplyNms(boxes, 0.45f);

            var rankedItems = nmsBoxes
                .OrderByDescending(x => x.Confidence)
                .Take(20)
                .Select(box =>
                {
                    var className = box.ClassIndex >= 0 && box.ClassIndex < classNames.Length
                        ? classNames[box.ClassIndex]
                        : $"class_{box.ClassIndex}";
                    var produkt = produktListe.Resolve(className);

                    return new DetectedItem
                    {
                        Navn = produkt.Navn,
                        Kode = produkt.Kode,
                        Type = produkt.Type,
                        Confidence = box.Confidence,
                        X = box.X,
                        Y = box.Y,
                        Width = box.Width,
                        Height = box.Height
                    };
                })
                .ToList();

            return rankedItems
                .GroupBy(x => x.Navn)
                .Select(group => new DetectedItem
                {
                    Navn = group.Key,
                    Kode = group.First().Kode,
                    Type = group.First().Type,
                    Confidence = group.Max(x => x.Confidence),
                    X = group.OrderByDescending(x => x.Confidence).First().X,
                    Y = group.OrderByDescending(x => x.Confidence).First().Y,
                    Width = group.OrderByDescending(x => x.Confidence).First().Width,
                    Height = group.OrderByDescending(x => x.Confidence).First().Height
                })
                .OrderByDescending(x => x.Confidence)
                .Take(10)
                .ToList();
        }
        private static List<YoloBox> ApplyNms(List<YoloBox> boxes, float threshold)
        {
            var result = new List<YoloBox>();

            var sortedBoxes = boxes
                .OrderByDescending(x => x.Confidence)
                .ToList();

            while (sortedBoxes.Count > 0)
            {
                var bestBox = sortedBoxes[0];
                result.Add(bestBox);
                sortedBoxes.RemoveAt(0);

                sortedBoxes = sortedBoxes
                    .Where(box => box.ClassIndex != bestBox.ClassIndex ||
                    CalculateIoU(bestBox, box) < threshold)
                    .ToList();
            }
            return result;
        }
        private static float CalculateIoU(YoloBox a, YoloBox b)
        {
            var x1 = Math.Max(a.X, b.X);
            var y1 = Math.Max(a.Y, b.Y);
            var x2 = Math.Min(a.X + a.Width, b.X + b.Width);
            var y2 = Math.Min(a.Y + a.Height, b.Y + b.Height);

            var intersectionWidth = Math.Max(0, x2 - x1);
            var intersectionHeight = Math.Max(0, y2 - y1);

            var intersectionArea = intersectionWidth * intersectionHeight;

            var areaA = a.Width * a.Height;
            var areaB = b.Width * b.Height;

            var unionArea = areaA + areaB - intersectionArea;

            return unionArea <= 0 ? 0: intersectionArea / unionArea;
        }
        private static float NormalizeScore(float score)
        {
            return score is >= 0f and <= 1f ? score : 1f / (1f + MathF.Exp(-score));
        }

        private static float[] Softmax(float[] values)
        {
            if (values.Length == 0)
                return Array.Empty<float>();

            var max = values.Max();
            var exps = new float[values.Length];
            float sum = 0f;

            for (var i = 0; i < values.Length; i++)
            {
                var exp = MathF.Exp(values[i] - max);
                exps[i] = exp;
                sum += exp;
            }

            if (sum <= 0f)
                return values.Select(_ => 0f).ToArray();

            for (var i = 0; i < exps.Length; i++)
            {
                exps[i] /= sum;
            }

            return exps;
        }

        private string[] GetClassNames(InferenceSession session)
        {
            var metadata = session.ModelMetadata.CustomMetadataMap;
            if (metadata == null || metadata.Count == 0)
                return standardClassNames;

            if (!metadata.TryGetValue("names", out var rawNames) || string.IsNullOrWhiteSpace(rawNames))
                return standardClassNames;

            var parsed = ParseClassNames(rawNames);
            return parsed.Length == standardClassNames.Length ? parsed : standardClassNames;
        }

        private static string[] ParseClassNames(string rawNames)
        {
            try
            {
                var dictionary = JsonSerializer.Deserialize<Dictionary<string, string>>(rawNames);
                if (dictionary != null && dictionary.Count > 0)
                {
                    return dictionary
                        .Select(pair => (Index: int.TryParse(pair.Key, out var value) ? value : int.MaxValue, Name: pair.Value))
                        .OrderBy(x => x.Index)
                        .Select(x => x.Name)
                        .Where(x => !string.IsNullOrWhiteSpace(x))
                        .ToArray();
                }
            }
            catch
            {
              
            }

            var pairMatches = Regex.Matches(rawNames, "(?:['\"])?(?<key>\\d+)(?:['\"])?\\s*:\\s*['\"](?<value>[^'\"]+)['\"]");
            if (pairMatches.Count > 0)
            {
                return pairMatches
                    .Select(match =>
                    {
                        var key = match.Groups["key"].Value;
                        var value = match.Groups["value"].Value;
                        return (Index: int.TryParse(key, out var parsed) ? parsed : int.MaxValue, Name: value);
                    })
                    .OrderBy(x => x.Index)
                    .Select(x => x.Name)
                    .Where(x => !string.IsNullOrWhiteSpace(x))
                    .ToArray();
            }

            var commaSeparated = rawNames
                .Split(',', StringSplitOptions.TrimEntries | StringSplitOptions.RemoveEmptyEntries)
                .ToArray();

            if (commaSeparated.Any(x => x.Contains(':')))
                return Array.Empty<string>();

            return commaSeparated;
        }

        private readonly record struct InputMode(bool NormalizeTo01, bool UseBgr);

    }
}
