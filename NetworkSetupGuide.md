# 쿠 멀티플레이어 게임 테스팅 설정 가이드

## 1️⃣ Unity 씬 설정하기

### NetworkManager 설정
1. **빈 게임오브젝트 생성**: "NetworkManager" 이름으로 생성
2. **NetworkManager 컴포넌트 추가**:
   - Component → Netcode → NetworkManager
   - 자동으로 UnityTransport도 함께 추가됩니다
3. **NetworkManager 설정**:
   - Connection Approval: ✅ 체크
   - Max Connections: 6

### NetworkGameManager 설정  
1. **빈 게임오브젝트 생성**: "NetworkGameManager" 이름으로 생성
2. **NetworkGameManager 스크립트 추가**:
   - Assets/Scripts/Network/NetworkGameManager.cs를 드래그
3. **NetworkObject 컴포넌트 추가**:
   - Component → Netcode → NetworkObject
   - Spawn With Observer: ✅ 체크

### UI 설정
1. **Canvas 생성**: UI → Canvas
2. **NetworkLobbyUI 설정**:
   - Canvas 하위에 빈 게임오브젝트 생성: "NetworkLobbyUI"  
   - NetworkLobbyUI 스크립트 추가
   - UI 버튼들 생성 (아래 참조)

## 2️⃣ UI 요소들 생성하기

### 연결 패널 (ConnectionPanel)
```
Canvas
├── ConnectionPanel
    ├── HostButton (Button)
    ├── ClientButton (Button) 
    ├── ServerButton (Button)
    ├── IPInputField (InputField)
    └── StatusText (Text)
```

### 로비 패널 (LobbyPanel)
```
Canvas  
├── LobbyPanel
    ├── PlayerCountText (Text)
    ├── StartGameButton (Button)
    ├── DisconnectButton (Button)
    └── PlayerListText (Text)
```

## 3️⃣ 컴포넌트 연결하기

**NetworkLobbyUI Inspector에서:**
- Host Button → HostButton
- Client Button → ClientButton  
- Server Button → ServerButton
- IP Input Field → IPInputField
- Status Text → StatusText
- Lobby Panel → LobbyPanel GameObject
- Connection Panel → ConnectionPanel GameObject
- Player Count Text → PlayerCountText
- Start Game Button → StartGameButton
- Disconnect Button → DisconnectButton
- Player List Text → PlayerListText

## 4️⃣ 테스트 방법

### 로컬 테스트 (1대의 PC에서)
1. **빌드 생성**: File → Build Settings → Build
2. **Unity Editor**에서 플레이 시작
3. **빌드된 실행파일** 실행
4. **한쪽에서 Host 선택**, 다른 쪽에서 Client 선택

### 네트워크 테스트 (여러 PC)
1. **호스트 PC**: Host 버튼 클릭
2. **클라이언트 PC들**: 호스트 IP 입력 후 Client 버튼 클릭
3. **최소 2명** 접속 후 Start Game 버튼 활성화

## 5️⃣ 게임플레이 테스트 포인트

- ✅ 플레이어 접속/퇴장
- ✅ 게임 시작 (2-6명)  
- ✅ 턴제 시스템
- ✅ 기본 액션들 (Income, Foreign Aid, Tax, Coup)
- ✅ 캐릭터 액션들 (Assassinate, Steal, Exchange)
- ✅ 챌린지/블록 시스템
- ✅ 게임 승리 조건

## 6️⃣ 디버그 방법

### Console 로그 확인
- Unity Console에서 네트워크 연결 상태 확인
- 게임 액션 로그 확인

### Inspector 디버깅  
- NetworkGameManager의 Context Menu 사용
- GameManager의 테스트 메서드들 활용

## 🚨 주의사항
- 방화벽에서 Unity Editor 허용 필요
- 같은 네트워크에 있어야 함 (공유기 내부)
- 포트 7777이 열려있어야 함