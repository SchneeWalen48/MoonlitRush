<div align="center">

# 🎮 Moonlit Rush 
### 물리 기반 3D 캐주얼 레이싱 게임
<a href="https://youtu.be/LPo_mc8xSBM">
  <img width="100" height="100" alt="Youtube_logo"
    src="https://github.com/user-attachments/assets/2aa6f449-7ffa-4dd2-9086-232f5499456f" />
</a>

<br><br>

<table>
  <tr>
    <td align="center" width="33%">
      <div style="position: relative; width: 100%; padding-top: 56.25%;">
      <img alt="Suspension_img" src="https://github.com/user-attachments/assets/e52f8670-5a16-4ff4-b1d9-f50617636d9f" width="100%" /></div>
      <br/>
      <b>서스펜션(Raycast)</b>
    </td>
    <td align="center" width="33%">
      <div style="position: relative; width: 100%; padding-top: 56.25%;">
      <img alt="Drift_img" src="https://github.com/user-attachments/assets/b83a773e-0470-4785-8b84-4b53a5ccb96a" width="100%" /></div>
      <br/>
      <b>드리프트</b>
    </td>
    <td align="center" width="33%">
      <div style="position: relative; width: 100%; padding-top: 56.25%;">
      <img alt="Item_img" src="https://github.com/user-attachments/assets/484b64fb-3e33-43d6-946b-3e15ecd306e0" width="100%" /></div>
      <br/>
      <b>아이템(2-Slot FIFO)</b>
    </td>
  </tr>
</table>

<br>

🚗 **Rigidbody 기반의 물리 연산으로 직접 구현한 아케이드 레이싱 게임**  
🚗 **서스펜션·드리프트·아이템 시스템을 포함한 차량 주행 시스템 중심 설계**

</div>

<br><br><br>

---

## 📋 Table of Contents

- [개요(Overview)](#overview)
- [아키텍처(Architecture)](#architecture)
- [차량 시스템(Vehicle Systems)](#vehicle-system)
  - [서스펜션](#suspension)
  - [조향 & 드리프트](#drift)
  - [자동 변속 기어](#gear)
  - [에어본](#airborne)
  - [배럴롤](#barrelroll)
- [아이템(Item System)](#item)
  - [미사일](#missile)
  - [실드](#shield)
  - [부스터](#booster)
- [순위 시스템(Ranking System)](#ranking-system)
- [개발자(Developer)](#developer)
  
<br><br>

---

<a id="overview"></a>
## 🎯 Overview

<strong>Moonlit Rush</strong>는 **캐주얼 아케이드 스타일**로, 플레이어의 주행을 WheelCollider 없 구현한 것이 특징인 **3D 레이싱 게임**입니다.

- 플랫폼: Windows
- 개발 엔진: Unity 2022.3
- 개발 기간: 2025.08.06 ~ 2025.08.25  
- 프로젝트 형태: 팀 프로젝트 (3명)

> 본 README에는 팀 프로젝트 중 제가 맡은 **플레이어 차량, 아이템, 랭킹 시스템** 파트가 정리되어 있습니다.

<br><br>

---

<a id="architecture"></a>
## 🚗 System Architecture

### 차량 시스템 흐름도

#### 📂 Source Entry
- [`/Assets/_Proj/Scripts`](https://github.com/SchneeWalen48/MoonlitRush/blob/main/Assets/_Proj/Scripts)

CarController는 입력을 기반으로 주행 상태를 판단하고,
서스펜션·기어·드리프트·에어본·배럴롤을 조건적으로 적용한 뒤
물리 힘을 Rigidbody에 반영하는 단일 흐름 구조로 설계되었습니다.

<div align="center"><a href="https://github.com/user-attachments/assets/e0d9d0d7-aa25-49ae-8133-560c0d423293"><img width="80%" alt="car physics structure" src="https://github.com/user-attachments/assets/e0d9d0d7-aa25-49ae-8133-560c0d423293" /></a></div>

<br>

---

### CarController 내부 구조도
<div align ="center"><a href="https://github.com/user-attachments/assets/660acb12-ae22-42f5-a65b-1a4f1907a696"><img width="435" alt="main drive pipeline" src="https://github.com/user-attachments/assets/660acb12-ae22-42f5-a65b-1a4f1907a696"></a><br><b>메인 주행 파이프라인</b></div>

<br>
<table>
  <tr>
    <td width="49%" align="center">
      <a href="https://github.com/user-attachments/assets/c0dc7df5-3d71-4266-a605-a5663836bfe6"><img alt="trigger sturcture" src="https://github.com/user-attachments/assets/c0dc7df5-3d71-4266-a605-a5663836bfe6" width="100%" /></a>
      <br/><br/><b>트리거 구조</b>
    </td>
    <!-- 세로 구분선 -->
    <td width="2%" align="center">
      <div style="width:1px; height:100%; background-color:#cccccc;"></div>
    </td>
    <td width="49%" align="center">
      <a href="https://github.com/user-attachments/assets/24efb913-3f9c-48a4-a198-d116c24c0785"><img alt="barrelroll flow" src="https://github.com/user-attachments/assets/24efb913-3f9c-48a4-a198-d116c24c0785" width="100%" /></a>
      <br/><b>배럴롤 흐름</b>
    </td>
  </tr>
</table>
<br>

---

### 아이템 구조도
<div align="center"><a href="https://github.com/user-attachments/assets/ff582bc9-d9b4-4549-9c0c-9e54adea8581"><img height="80%" alt="item system structure" src="https://github.com/user-attachments/assets/ff582bc9-d9b4-4549-9c0c-9e54adea8581" /></a></div>

<br>

---

### 랭킹 구조도
<div align="center"><a href="https://github.com/user-attachments/assets/4580f70b-dc05-4f80-9486-85a6a52d7da0"><img width="80%" alt="ranking structure" src="https://github.com/user-attachments/assets/4580f70b-dc05-4f80-9486-85a6a52d7da0" /></a></div>

<br>

---

<a id="vehicle-system"></a>
## 🚙 Vehicle System

#### 📂 Code Reference
- [`CarController.cs`](https://github.com/SchneeWalen48/MoonlitRush/blob/main/Assets/_Proj/Scripts/Player/CarController.cs)

<a id="suspension"></a>
### 🔩 Suspension

💡 **Raycast 기반 서스펜션 및 접지 판정 시스템**

- **주요 기능**
  - Raycast를 이용한 접지 판정
  - 서스펜션 압축 비율 기반 반력 계산
  - 댐핑을 적용한 진동 완화
  - WheelCollider 없이 Rigidbody 기반 주행 안정성 확보
- **주요 메서드**
  - Suspension()

<br>

---

<a id="drift"></a>
### 🛞 Steering & Drift

💡 **횡저항 제어를 통한 드리프트 시스템**

- **주요 기능**
  - 차량 횡속도 기반 횡저항 조절
  - 조향 입력에 따른 회전력 적용
  - 드리프트 시 미끄러짐을 허용하면서도 제어 가능하도록 설계

- **주요 메서드**
  - SidewaysDrag()
  - Turn()

<br>

---

<a id="gear"></a>
### 🛻 Gear

💡 **수동 기어를 근사한 자동 변속 시스템**

- **주요 기능**
  - 다단 기어 기반 자동 변속
  - 기어별 최대 속도 도달 시 상위 기어로 전환
  - 변속 시 순간적인 속도 보정으로 수동 변속 감각을 근사
- **주요 메서드**
  - GearLogic()
  - ApplyGearHoldAndCap()

<br>

---

<a id="airborne"></a>
### 🛬 Airborne

💡 **공중에서 차체 안정화를 위한 로직**

- **주요 기능**
  - 공중 진입 시 접지력 상실 처리
  - 중력 및 회전 보정을 통한 공중 자세 안정화
- **주요 메서드**
  - Airbourne()

<br>

---

<a id="barrelroll"></a>
### 🛢 Barrel Roll

💡 **캐주얼 레이싱 감각을 위한 배럴롤 액션**

- **주요 기능**
  - 공중 진입 후 차량 전방 축 기준 회전
  - 배럴롤 중 전진 속도 유지 및 부스트 적용
- **주요 메서드**
  - BarrelRollCoroutine()
- **유사 기믹**
  - BoostPad
  - SpeedSlope

<br>

---

<a id="item"></a>
## 🧰 Item System

#### 📂 Code Reference
- [`/Scripts/Items`](https://github.com/SchneeWalen48/MoonlitRush/blob/main/Assets/_Proj/Scripts/Items)

💡 **2-Slot FIFO 구조의 아이템 시스템**

  - 최대 2개의 아이템을 슬롯에 저장 가능
  - 아이템은 선입선출(FIFO) 방식으로 관리
  - 아이템 사용 시, 뒤 슬롯의 아이템이 앞으로 이동하여 순서 꼬임 방지

<a id="missile"></a>
### 🚀 Missile

#### [`MissileProj.cs`](https://github.com/SchneeWalen48/MoonlitRush/blob/main/Assets/_Proj/Scripts/Items/MissileProj.cs)
💡 **반경 내 가장 가까운 차량을 추적하는 미사일**

  - 반경 내 플레이어 또는 AI를 탐색하여 전방 기준 가장 가까운 차량을 타겟으로 설정
  - 타겟이 없을 경우 직선으로 이동 후 일정 조건에서 소멸
  - 피격된 차량은 속도 감소 및 공중으로 튀어 오르는 효과 적용

<br>

<a id="shield"></a>
### 🔰 Shield

#### [`ShieldItem.cs`](https://github.com/SchneeWalen48/MoonlitRush/blob/main/Assets/_Proj/Scripts/Items/ShieldItem.cs)
💡 **미사일 공격을 무효화하는 방어 아이템**

  - 사용 시 일정 시간 동안 미사일 충돌을 무시하는 보호 상태 적용

<br>

---

<a id="booster"></a>
### ⚡ Booster

#### [`BoosterItem.cs`](https://github.com/SchneeWalen48/MoonlitRush/blob/main/Assets/_Proj/Scripts/Items/BoosterItem.cs)
💡 **속도 상태를 보정하는 부스터 아이템**

  - 부스트 효과(FX)와 함께 일정 시간 가속 상태 연출
  - 현재 속도가 낮을 경우 최소 전진 속도를 보장하여 출발 및 회복 구간 보조
  - 차량의 최고 속도 제한을 초과하지 않도록 설계
  - 속도 상태에 따라 체감 효과가 달라지도록 구성

<br>

---

<a id="ranking-system"></a>
## 🏁 Ranking System

#### 📂 Code Reference
- [`/Scripts/Checkpoint`](https://github.com/SchneeWalen48/MoonlitRush/blob/main/Assets/_Proj/Scripts/Checkpoint)
- [`LapCounter.cs`](https://github.com/SchneeWalen48/MoonlitRush/blob/main/Assets/_Proj/Scripts/LapSystem/LapCounter.cs)
- [`RacerInfo.cs`](https://github.com/SchneeWalen48/MoonlitRush/blob/main/Assets/_Proj/Scripts/LapSystem/RacerInfo.cs)
- [`RaceRankUI.cs`](https://github.com/SchneeWalen48/MoonlitRush/blob/main/Assets/_Proj/Scripts/LapSystem/RaceRankUI.cs)
- [`TimeManager.cs`](https://github.com/SchneeWalen48/MoonlitRush/blob/main/Assets/_Proj/Scripts/Managers/TimeManager.cs)
- [`RaceManager.cs`](https://github.com/SchneeWalen48/MoonlitRush/blob/main/Assets/_Proj/Scripts/Managers/RaceManager.cs)
- [`RankingUI.cs`](https://github.com/SchneeWalen48/MoonlitRush/blob/main/Assets/_Proj/Scripts/UI/RankingUI.cs)

💡 **체크포인트 기반 실시간 순위 산정 시스템**

### 🥇 System Overview


💡 **체크포인트 진행도를 기준으로 실시간 순위를 산정하고, 결승선 통과 시 기록과 순위를 결과 화면으로 연결**

  - 각 차량은 `Checkpoint` 통과 시 진행 상태를 갱신
  - `RacerInfo`에 각 차량의 현재 랩 수와 체크포인트 진행도가 저장됨
  - `RaceManager`가 모든 차량의 진행 상태를 비교하여 실시간 순위 산정
  - `RaceRankUI`는 레이스 중 `RaceManager`의 순위 정보를 참조하여 HUD에 실시간 순위 표시
  - `TimeManager`는 주행 시간 측정 및 결승선 통과 시 기록을 저장
  - 레이스 종료 시, `RankingUI`가 최종 순위 및 기록 데이터를 결과 화면에 출력

<br>

---

<a id="developer"></a>
## 👨‍💻 개발자
<div align="center">

**김현지**

<br>

<a href="https://github.com/SchneeWalen48">
  <img src="https://img.shields.io/badge/SchneeWalen48-blue?style=for-the-badge&logo=GitHub&logoColor=ffffff&label=GitHub&labelColor=Black"/>
</a>

<br><br>

**Moonlit Rush** <br>
Raycast 서스펜션과 물리 기반 차량 주행 시스템을 중심으로 <br/>
아이템 및 실시간 순위 시스템까지 구현한 3D 레이싱 게임 프로젝트

<br>

물리 기반 차량 주행 시스템 구현<br/>
2-Slot FIFO 아이템 시스템 및 실시간 순위 시스템 개발<br/>
전체 UX 검수

</div>
