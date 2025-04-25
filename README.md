```markdown
# PolarTQR

A simple mobile app for scanning tickets at events using .NET MAUI.

## 📱 Project Overview

This app allows event staff to:
- **Log in** using a dummy admin account.
- **Select an event** to control tickets for.
- **Open the camera** and display captured images (QR scanning to be added later).

---

## 🔧 Features Implemented

- Login Page:
  - Username: `admin`
  - Password: `1234`
- Event Selection Page with dummy events.
- Camera access and image preview (works on physical devices only).
- Simulated ticket scan functionality.

---

## 🛠️ Project Structure

```
/PolarTQR
│
├── /Models
│   └── User.cs
│   └── Event.cs
│
├── /ViewModels
│   └── LoginViewModel.cs
│   └── HomeViewModel.cs
│   └── ScanViewModel.cs
│
├── /pages
│   └── LoginPage.xaml (+ .cs)
│   └── HomePage.xaml (+ .cs)
│   └── ScanPage.xaml (+ .cs)
│
├── App.xaml
├── App.xaml.cs
└── README.md
```

---

## 🚀 Getting Started

### Requirements:
- Visual Studio 2022/2023 with MAUI workloads.
- .NET 7 or 8 SDK.
- Android device or emulator (camera testing requires a physical device).

### Build & Run:
1. Clone this repository.
2. Open the solution in Visual Studio.
3. Set **PolarTQR** as the startup project.
4. Run on Android/iOS.

---

## 📸 Camera Testing
> **Note:** Camera functionality requires a physical device. MediaPicker does not work on emulators.

- Make sure to add necessary permissions:
  - **Android**: In `AndroidManifest.xml`
    ```xml
    <uses-permission android:name="android.permission.CAMERA" />
    ```
  - **iOS**: In `Info.plist`
    ```xml
    <key>NSCameraUsageDescription</key>
    <string>Camera is required to scan tickets</string>
    ```

---

## 🔜 Roadmap

- [ ] Implement QR code scanning (ZXing.Net.MAUI).
- [ ] Connect login and events to real API.
- [ ] Validate tickets against backend service.
- [ ] Add event-specific ticket tracking.

---

## 📫 Contact

Feel free to reach out for questions or contributions!

---

```
