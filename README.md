# IoT-Based Smart Home System 🏠

![Project Status](https://img.shields.io/badge/Status-In%20Development-orange)
![Course](https://img.shields.io/badge/Course-CENG%20407%2F408-blue)
![University](https://img.shields.io/badge/University-Cankaya%20University-red)

An **Edge-First**, AI-supported Smart Home System designed for **CENG 407/408 Senior Design Project**. This project integrates environmental monitoring, automated resource management, and intelligent security features into a cohesive IoT ecosystem.

---

## 👥 Group Members

| Student Name | Student ID |
|--------------|------------|
| İbrahim Ersan Özdemir | 202211054 |
| Deniz Arda Çınarer | 202211019 |
| Duhan Ayberk Seven | 202211062 |
| Enis Mirsad Şengül | 202211065 |

---

## 📖 Project Overview

Commercial smart home systems often operate as "closed ecosystems," prioritizing convenience over privacy and locking users into specific brands. Our project bridges this gap by proposing a customizable, **Privacy-First** architecture.

**Key Philosophy:**
* **Edge-First:** Critical processing (e.g., fire detection, face recognition pre-processing) happens locally on the Raspberry Pi 5.
* **Graceful Degradation:** The system continues to perform essential security functions even if the internet connection is lost.
* **Hybrid Cloud:** Uses the cloud for heavy lifting (Face Matching), data persistence, and remote mobile access.

---

## 🏗 System Architecture

The system follows a **4-Tier Layered Architecture** ensuring separation of concerns:

1.  **Edge Layer (Raspberry Pi 5):** Handles sensors, local alarms, and initial AI processing.
2.  **Cloud Layer:** Manages database, API endpoints, and push notifications.
3.  **Client Layer:** Mobile App and Web Dashboard for user interaction.
4.  **Hardware Layer:** Sensors and Actuators.

> **Architecture Diagram:**
> ![System Architecture](https://raw.githubusercontent.com/CankayaUniversity/ceng-407-408-2025-2026-A-Smarthome-System/main/images/Design%20of%20Home%20Controller.png)

---

## ✨ Key Features (Epics)

### 1. Environmental & Resource Monitoring 🌡️
* **Climate Tracking:** Real-time Temperature & Humidity monitoring via DHT11 sensors.
* **Pet Resource Automation:** Tracks food/water bowl weight using Load Cells (HX711) and alerts when low (<10%).
* **Plant Health:** Monitors soil moisture to prevent dehydration.
* **Energy Efficiency:** Tracks light usage duration via LDR sensors to detect energy waste.

### 2. Critical Safety & Hazards 🚨
* **Interrupt-Based Detection:** Immediate response (<100ms) for **Fire (Smoke/Gas)** and **Flood** events.
* **Local Alarms:** Triggers buzzer locally regardless of internet status.

### 3. AI-Supported Security 🧠
* **Motion-Sensitive Capture:** PIR sensors wake up the camera to save energy and storage.
* **Edge Intelligence:** Detects human presence locally.
* **Face Recognition:** Identifies "Authorized" family members vs. "Unauthorized" intruders via Cloud AI.
* **Privacy:** Does not stream video 24/7; only processes relevant security events.

### 4. Remote Access & Data 📱
* **Mobile App:** Push notifications for critical alerts (latency < 5s).
* **Web Dashboard:** Historical data visualization (Time-series charts).
* **Access Control:** Register new family members using Face ID.

---

## 🛠 Hardware & Tech Stack

**Hardware Components:**
* **Controller:** Raspberry Pi 5 (8GB)
* **Sensors:** DHT11 (Temp/Hum), MQ-2 (Gas/Smoke), PIR (Motion), Load Cell + HX711 (Weight), Capacitive Soil Moisture, LDR (Light).
* **Output:** 7-inch Touch Display (Local Dashboard), Buzzer.

**Software Stack:**
* **Edge:** Python, OpenCV / TensorFlow Lite.
* **Backend:** RESTful API (Python Flask/Django or Node.js).
* **Database:** Cloud-hosted SQL/NoSQL Database.
* **Mobile:** Flutter / React Native.
* **Communication:** HTTPS (TLS 1.2+), MQTT.

---

## 📸 User Interfaces

| Mobile Dashboard | Web Dashboard |
|:----------------:|:-------------:|
| ![Mobile App](https://raw.githubusercontent.com/CankayaUniversity/ceng-407-408-2025-2026-A-Smarthome-System/main/images/Mobil.png) | ![Web Dashboard](https://raw.githubusercontent.com/CankayaUniversity/ceng-407-408-2025-2026-A-Smarthome-System/main/images/Web.png) |
| *Real-time status tracking* | *Historical data & logs* |

*(Upload screenshots from your Requirements PDF here)*

---

## 📜 License
This project is developed for academic purposes at Çankaya University, Department of Computer Engineering.
