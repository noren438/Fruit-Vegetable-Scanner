# 🍎 Fruit and Vegetable Scanner

An offline AI-powered mobile application that detects fruits and vegetables from images and displays the predicted product name, confidence score, and product information.

The application was developed with .NET MAUI Blazor Hybrid and uses a locally stored ONNX object-detection model, allowing predictions to run without an internet connection.

## 🛠️ Technologies

![C#](https://img.shields.io/badge/C%23-512BD4?style=for-the-badge&logo=csharp&logoColor=white)
![.NET MAUI](https://img.shields.io/badge/.NET_MAUI-512BD4?style=for-the-badge&logo=dotnet&logoColor=white)
![Blazor](https://img.shields.io/badge/Blazor_Hybrid-512BD4?style=for-the-badge&logo=blazor&logoColor=white)
![ONNX](https://img.shields.io/badge/ONNX_Runtime-005CED?style=for-the-badge&logo=onnx&logoColor=white)
![YOLOv8](https://img.shields.io/badge/YOLOv8-AI-00FFFF?style=for-the-badge)
![Python](https://img.shields.io/badge/Python-3776AB?style=for-the-badge&logo=python&logoColor=white)

## ✨ Features

- Offline fruit and vegetable detection
- Local ONNX model inference
- Camera photo capture
- Image selection from the device
- Multiple-object detection
- Confidence-score display
- Product name and product-code display
- Cross-platform .NET MAUI application

## 🧠 AI Model

The object-detection model was trained using YOLOv8 and exported to ONNX format.

The model is stored locally at:

```text
Resources/Raw/best.onnx
```

This allows the application to perform predictions without sending images to an external server.

## 🏗️ Project Structure

```text
Components/
├── Layout/
└── Pages/

Models/
├── DetectedItem.cs
├── DetectionResults.cs
├── ProduktInfo.cs
└── YoloBox.cs

Services/
├── CameraService.cs
├── OnnxDetectorService.cs
├── OnnxModelProviderService.cs
└── ProduktService.cs

Resources/
└── Raw/
    └── best.onnx

Platforms/
wwwroot/
```

## 🚀 Run the Project

### Requirements

- Visual Studio 2022 or newer
- .NET MAUI workload
- .NET 10 SDK
- Android emulator, Windows machine, or supported mobile device

### Clone the repository

```bash
git clone https://github.com/noren438/Fruit-Vegetable-Scanner.git
```

### Open the project

Open:

```text
Frugt_Groent_Scanner.sln
```

in Visual Studio.

Restore dependencies and build the solution:

```bash
dotnet restore
dotnet build
```

Select a target platform and run the application.

## 📸 Screenshots

Screenshots will be added here.

## 👩‍💻 Author

**Noren Jensen**

- GitHub: https://github.com/noren438
- LinkedIn: https://www.linkedin.com/in/noren-galsim-jensen/

## 📄 License

This project was created for educational and portfolio purposes.