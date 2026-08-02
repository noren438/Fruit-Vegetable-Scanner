using Microsoft.ML.OnnxRuntime;

namespace Frugt_Grønt_Scanner.Services
{
    public class OnnxModelProviderService : IDisposable
    {
        private readonly SemaphoreSlim slim = new(1, 1);
        private InferenceSession? session;

        public async Task<InferenceSession> GetSessionAsync()
        {
            if (session != null)
            
                return session;
            
            await slim.WaitAsync();

            try
            {
                if (session != null)

                    return session;

                var modelPath = Path.Combine(FileSystem.CacheDirectory, "best.onnx");

                if (!File.Exists(modelPath))
                {
                    await using var source = await FileSystem.OpenAppPackageFileAsync("best.onnx");
                    await using var destination = File.Create(modelPath);
                    await source.CopyToAsync(destination);
                }


                var options = new SessionOptions
                {
                    GraphOptimizationLevel = GraphOptimizationLevel.ORT_ENABLE_EXTENDED,
                    ExecutionMode = ExecutionMode.ORT_SEQUENTIAL,
                    InterOpNumThreads = 1,
                    IntraOpNumThreads = Math.Max(1, Environment.ProcessorCount / 2)
                };

                options.EnableMemoryPattern = true;
                options.EnableCpuMemArena = true;

                session = new InferenceSession(modelPath, options);

                return session;
            }
            catch (FileNotFoundException ex)
            {
                throw new Exception(
                    $"ONNX modellen blev ikke fundet. {ex.Message}", ex);
            }
            catch (OnnxRuntimeException ex)
            {
                throw new Exception(
                    $"Fejl ved indlæsning af ONNX modellen. {ex.Message}", ex);
            }
            catch (Exception ex)
            {
                throw new Exception(
                    $"Ukendt fejl i OnnxModelProviderService. {ex.Message}", ex);
            }
            finally
            {
                slim.Release();
            }
        }
        public void Dispose()
        {
            session?.Dispose();
            slim.Dispose();
        }
    }
}
