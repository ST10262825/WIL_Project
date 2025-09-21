<div align="center">
  <h1> TutorConnect </h1>
</div>

<div align="center">

  <h3>Smart Tutor Booking Made Simple</h3>

  <p>A comprehensive tutor booking system developed by Klante Softwares, providing students, tutors, and administrators with a reliable platform to manage tutoring sessions, availability, and performance dashboards.</p>

  ![ASP.NET MVC](https://img.shields.io/badge/ASP.NET%20MVC-Web%20Frontend-blue?style=for-the-badge&logo=dotnet)
  ![Android](https://img.shields.io/badge/Android-Kotlin-green?style=for-the-badge&logo=android)
  ![.NET Core](https://img.shields.io/badge/.NET%20Core-API%20Backend-purple?style=for-the-badge&logo=dotnet)
  ![SQL Server](https://img.shields.io/badge/SQL%20Server-Database-red?style=for-the-badge&logo=microsoftsqlserver)

</div>

---

## 📖 Overview

The *TutorConnect* is a smart tutor booking system that enables seamless interaction between students, tutors, and administrators. This comprehensive platform manages tutoring sessions, availability scheduling, and provides detailed performance analytics through integrated dashboards.

This application was developed by **Klante Softwares** for the **WIL Project**.

---

## ✨ Features

### 👥 Team Members
- Lindokuhle Moyana  
- Christinah Chitombi  
- Keira Wilmot   
- Nqobani Moyane  
- Yashna Komla  

### 🎓 Student Features
- **Account Registration**: Create and manage student profiles  
- **Tutor Browsing**: Search and view available tutors  
- **Session Booking**: Schedule tutoring sessions  
- **Notification System**: Receive real-time updates  
- **Chat Integration**: Direct communication with tutors  

### 👨‍🏫 Tutor Features
- **Availability Management**: Set and update availability schedules  
- **Session Check-in**: Track session attendance  
- **Hour Logging**: Record and monitor worked hours  
- **Student Communication**: Integrated chat system  

### 🛠️ Administrator Features
- **Tutor Registration**: Manage tutor profiles and onboarding  
- **Performance Monitoring**: Track tutor effectiveness  
- **Report Generation**: Create detailed analytics reports  
- **Dashboard Access**: Comprehensive system overview  

---

## ⚙️ Prerequisites

| Requirement | Version / Notes |
|-------------|-----------------|
| **.NET SDK** | 9.0 |
| **Database** | SQL Server (local or cloud) |
| **IDE** | Visual Studio 2022 or JetBrains Rider |
| **Mobile Development** | Android Studio (for Kotlin app) |
| **Firebase** | Project setup required |

---

## 🚀 How to Compile and Run

### 1. Clone the Repository
```bash
git clone https://github.com/ST10262825/WIL_Project.git
cd WIL_Project
```

### 2. Configure Database
- Update connection string in `appsettings.json` with SQL Server credentials  
- Run database migrations or create schema manually  

### 3. Configure Firebase
- Add Firebase configuration (API keys, project settings)  
- Include `google-services.json` for Android app  

### 4. Build the Solution
```bash
dotnet restore
dotnet build
```

### 5. Run the Application
```bash
dotnet run
```
- The app will run on `https://localhost:5001` by default  

---

## 📱 How to Use the Application

### Web Application
1. **Students**: Register → Browse Tutors → Book Sessions → Receive Notifications  
2. **Tutors**: Login → Set Availability → Check into Sessions → Log Hours  
3. **Admins**: Manage Tutors → Monitor Performance → Generate Reports  

### Mobile Application (Android - Kotlin)
1. Open TutorConnect project in **Android Studio**  
2. Configure Firebase in `google-services.json`  
3. Run on emulator or physical device  

---

## 📂 Project Structure

```
WIL_Project/
│
├── Web Frontend/           # ASP.NET MVC application
├── Mobile App/            # Android Kotlin application  
├── API Backend/           # .NET Core API services
├── Database/              # SQL Server schema and migrations
├── Firebase Config/       # Firestore and FCM configuration
├── Power BI Reports/      # Dashboard and analytics files
└── README.md             # Documentation
```

## 🗄️ Technology Stack Used

### Frontend Technologies
- **ASP.NET MVC**: Web-based user interface  
- **Android (Kotlin)**: Mobile application development  

### Backend Technologies
- **.NET Core API**: RESTful services with JWT authentication  
- **SQL Server**: Structured data storage  
- **Firebase Firestore**: Real-time chat and updates  
- **Firebase Cloud Messaging (FCM)**: Push notifications  

### Business Intelligence
- **Power BI**: Dashboards and performance analytics  

---

## 🎯 User Engagement Strategy

The system implements a **comprehensive engagement approach**:  
- **Real-time Notifications**: Keep users informed of session updates  
- **Performance Dashboards**: Provide tutors and admins with actionable insights  
- **Integrated Communication**: Seamless chat between students and tutors  
- **Role-based Access**: Tailored experiences for different user types  

---

## 🔒 Security Implementation

| Security Feature | Implementation |
|------------------|----------------|
| **Authentication** | Firebase Auth + JWT tokens |
| **Authorization** | Role-based access control |
| **Data Protection** | Secure API endpoints |
| **Session Management** | Token-based authentication |

---

## 🏗️ Architecture Overview

### System Components
- **Students, Tutors, and Admins** interact with:
  - ASP.NET MVC Web Application  
  - Android Mobile Application  
  - .NET Core API Backend  
  - SQL Server (structured data storage)  
  - Firebase Firestore (real-time features)  
  - Firebase Cloud Messaging (notifications)  
  - Power BI (analytics and dashboards)  

---

## 📊 Reporting and Analytics

### Admin Dashboard Features
- **Tutor Performance Metrics**: Track effectiveness and engagement  
- **Student Activity Analysis**: Monitor booking patterns and usage  
- **System Usage Statistics**: Overall platform performance  
- **Custom Report Generation**: Tailored analytics for stakeholders  

### Power BI Integration
- Real-time data visualization  
- Interactive dashboards  
- Performance trend analysis  
- Export capabilities for stakeholders  

---

## 🎯 Client Information

**Client**: Academic Institution / VC  
**Target Users**: Students, Tutors, Administrators  
**Primary Goal**: Streamline tutoring service management and enhance educational support  

---

## 📌 Development Status

This project is currently under **active development**. 

### Current Status
- Core functionality implemented  
- User authentication and authorization complete  
- Database schema established  
- Mobile app in development  

---

**Repository**: [WIL_Project](https://github.com/ST10262825/WIL_Project)  
**Developer**: Klante Softwares  
**Last Updated**: September 21, 2025
