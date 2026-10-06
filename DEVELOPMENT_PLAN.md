# 개발 계획

CLAUDE.md의 규칙을 기준으로 한 단계별 개발 계획이다. 단계마다 경고 0개, 오류 0개로 빌드가 통과한 뒤 다음 단계로 넘어간다. 각 단계를 시작하기 전에 CLAUDE.md와 이 문서를 다시 읽고, 이전 단계 코드를 점검한다. 폴더와 클래스 이름은 잠정안이며 개발하면서 바뀔 수 있다.

## 추가 결정 사항

CLAUDE.md에 정해져 있지 않아 따로 정한 내용이다.

| 항목 | 결정 |
|---|---|
| 전체 깨우기 대상 | 목록의 PC 중 상태가 켜짐이 아닌 PC 전부 (꺼짐, 알 수 없음, 깨우는 중 포함). 누르면 전체 행을 선택해 보여 주고 확인창에서 예를 눌러야 전송. 모두 켜져 있으면 안내만 |
| 깨우는 중 재요청 | 다시 전송하고 깨우기 요청 시각을 새로 기록해 3분 대기를 다시 시작 |
| 꺼짐 상태 문구 | 화면 표시는 "응답 없음", 로그는 "Ping 응답 없음"으로 쓴다. 내부 상태 이름은 `Off` 유지 |
| 스캔 병합 IP 충돌 | MAC 일치로 IP를 갱신할 때 그 IP를 다른 항목이 쓰고 있으면 다른 항목의 IP를 비우고 로그에 남긴다 |
| IP 중복 | 편집 영역에서 다른 항목과 같은 IP는 등록/수정 불가 (빈 IP는 중복 검사 제외) |
| 삭제 확인 | 대화상자 서비스로 확인창을 띄우고 확인 시에만 삭제 |
| 로그 최대 줄 수 | 500줄, 화면에만 표시하고 파일로 남기지 않음 |
| 저장 파일 | 실행 파일 폴더의 `pcs.json` |
| 기본 목록 | 2026-10-06. 저장소의 `pcs.default.json`(설비 PC 목록)을 빌드 때 실행 파일 폴더로 복사. `pcs.json`이 없을 때만 불러오고, 첫 저장 때 `pcs.json`이 생긴다. 기본 목록 파일은 프로그램이 쓰지 않음 |
| 깨진 저장 파일 | 다음 저장이 덮어쓰지 않도록 `pcs.broken.json`으로 옮기고 빈 목록으로 시작 |
| 대상 체크박스 | 없앰 (2026-10-01). 행 선택은 편집, 행 버튼은 개별 깨우기, 전체 깨우기는 확인 후 전체로 역할을 나눔. 예전 파일의 isTarget은 무시 |
| 원격 끄기 | 2026-10-01 추가. 행의 끄기 버튼 + 확인창, USER 계정 빈 비밀번호로 IPC$ 연결 후 InitiateSystemShutdownEx, 강제 종료, 끄는 중 상태 3분 |
| 화면 개편 | 2026-10-01. 표 대신 PC 카드 격자(한 화면에 모두), 오른쪽 선택한 PC 패널(깨우기·끄기·수정·삭제), 새 PC 추가는 별도 창, 스캔은 한 줄로 압축 |
| 이름 조회 | 2026-10-01. 역방향 DNS + NetBIOS 동시 조회, IP 모양 이름만 스캔 때 자동 갱신 |
| 가상 어댑터 | VMware/VirtualBox/Hyper-V/vEthernet 어댑터 중 게이트웨이가 없는 것만 스캔 대역 목록과 깨우기 전송 NIC에서 제외 (Hyper-V 외부 스위치로 실제 망에 붙은 노트북 대비). 스캔 결과에서 자기 자신을 뺄 때는 가상 어댑터 주소까지 포함 |
| 스캔 전 대역 재확인 | 스캔 직전에 NIC 목록을 다시 읽고, 선택한 대역이 사라졌으면 스캔하지 않고 다시 선택하도록 안내 |

## 폴더 구조 (잠정)

```
WolManager/
  Mvvm/          ObservableObject, RelayCommand, AsyncRelayCommand
  Models/        PcEntry, PcStatus, PcRecord(저장용), SubnetInfo
  Services/      인터페이스와 구현 (저장소, 로그, 대화상자, 깨우기, 상태 확인, 네트워크, 스캔)
  Native/        IpHlpApi (SendARP P/Invoke)
  ViewModels/    MainViewModel, ScanViewModel, PcListViewModel, EditorViewModel, LogViewModel
  Views/         ScanView, PcListView, EditorView, LogView
  Controls/      StatusIndicator
  Resources/     Colors.xaml, Styles.xaml
  Constants.cs   포트, 전송 횟수, 타임아웃, 동시성, 확인 주기, 로그 최대 줄 수
```

## 상수

| 이름 | 값 |
|---|---|
| 매직 패킷 포트 | 9 |
| 매직 패킷 전송 횟수 / 간격 | 3회 / 100ms |
| 스캔 동시 요청 수 | 32 (64는 Wi-Fi에서 응답 누락) |
| 역방향 DNS 타임아웃 | 1.5초 |
| 상태 확인 주기 / Ping 타임아웃 | 5초 / 1초 |
| 깨우는 중 유지 시간 | 3분 |
| 스캔 최대 마스크 | /22 (더 넓으면 /24로 축소) |
| 로그 최대 줄 수 | 500 |

## 1단계: MVVM 기반, 공통 스타일, 앱 조립 뼈대

- `ObservableObject`: `INotifyPropertyChanged`, `SetProperty` 헬퍼
- `RelayCommand`: 동기 커맨드, `CanExecute` 지원, 재평가 요청 메서드
- `AsyncRelayCommand`: 실행 중 `CanExecute` false, 예외는 잡아서 생성자로 받은 오류 처리기(이후 로그 서비스)에 넘김
- `IDialogService` / `MessageBoxDialogService`: 경고, 확인(예/아니오)
- 리소스 사전: 배경, 카드, 테두리, 강조색, 상태 색상 4종, 글꼴(Malgun Gothic 13px, Consolas + Malgun Gothic 폴백), 카드/버튼/주 버튼 스타일. `App.xaml`에서 병합
- `App.xaml`의 `StartupUri` 제거, `OnStartup`에서 서비스 → ViewModel → `MainWindow` 순으로 수동 조립하고 `DataContext` 지정
- `MainWindow`: 4개 영역 자리를 위에서 아래로 배치 (영역 View는 이후 단계에서 채움)
- 커스텀 제목 표시줄(`WindowChrome`, 최소화/최대화/닫기)과 빈 영역 드래그 이동 (Attached Behavior)
- 기존 템플릿 파일을 file-scoped 네임스페이스로 바꾸고 불필요한 using 정리

완료 기준: 빌드 통과, 빈 창이 스타일이 적용된 상태로 뜬다.

## 2단계: PC 모델, 저장소 서비스, 로그 서비스와 로그 영역

- `PcStatus`: `Unknown`, `On`, `Waking`, `Off`
- `PcEntry`(ObservableObject): 이름, MAC, IP, 대상 여부, 상태, 깨우기 요청 시각
- `PcRecord`: 저장용 DTO (이름, MAC, IP, 대상 여부 `bool?`). 로드 시 값이 없으면 대상 아님
- `IPcRepository` / `JsonPcRepository`
  - `ObservableCollection<PcEntry> Items` 단일 소유
  - 로드, 추가, 수정, 삭제, 대상 변경 반영, 저장
  - `System.Text.Json`, 들여쓰기, 한글 비이스케이프(`JavaScriptEncoder.Create(UnicodeRanges.All)`)
  - 파일이 없거나 깨져 있으면 빈 목록으로 시작하고 로그 기록
- `ILogService` / `LogService`: 시간 붙여 기록, 최신이 맨 위, 500줄 초과분 제거, 다른 스레드에서 호출돼도 UI 스레드에서 컬렉션 변경
- `LogViewModel` + `LogView`: 로그 목록 표시 (Consolas)

완료 기준: 저장 파일이 없는 상태로 실행해도 예외 없이 뜨고, 로그 영역에 로드 결과가 표시된다.

## 3단계: PC 목록 영역

- `StatusIndicator` UserControl: 상태 점 + 텍스트, `Status` DependencyProperty로 값을 받음
- `PcListViewModel`: 저장소의 `Items` 바인딩, 선택 항목
- 대상 체크 변경은 저장소가 항목의 변경 알림으로 감지해 로그를 남기고 바로 저장 (목록 변경은 모두 저장소를 거친다는 규칙 유지)
- `PcListView`: 제목, "상태 새로고침", "전체 깨우기"(주 버튼) 자리, 열(대상, 상태, 이름, MAC, IP, 깨우기 버튼)
- 목록이 비어 있으면 "대상 PC를 모두 켠 뒤 네트워크 스캔을 누르세요" 안내 한 줄 표시
- 새로고침/깨우기 버튼은 5, 6단계에서 커맨드 연결
- 선택 변경을 외부에 알리는 이벤트 또는 프로퍼티 제공 (MainViewModel이 편집 영역과 연결)

완료 기준: `pcs.json`을 직접 작성해 넣으면 목록에 표시되고, 대상 체크 변경이 파일에 저장된다.

## 4단계: 편집 영역

- 입력 검증 유틸
  - MAC: 12자리 16진수, 또는 `-`/`:` 구분 6그룹만 허용, `AA-BB-CC-DD-EE-FF`로 정규화
  - IP: 비어 있거나 올바른 IPv4
  - 이름: 필수
- `EditorViewModel`: 이름/MAC/IP 입력, 새로 입력 / 추가 / 수정 / 삭제 커맨드
  - MAC 중복, IP 중복(빈 IP 제외) 검사
  - 검증 실패 시 대화상자 서비스로 경고, 저장하지 않음
  - 삭제 전 확인창
  - 모든 변경은 저장소 서비스를 통해 반영하고 직후 저장, 로그 기록
- `MainViewModel`에서 목록 선택 → 편집 영역 입력값 채우기 연결

완료 기준: 수동 추가/수정/삭제가 목록과 파일에 반영되고, 잘못된 MAC/IP는 경고 후 저장되지 않는다.

## 5단계: 매직 패킷 전송, 개별/전체 깨우기

- `INetworkInterfaceService`: Up 상태, Loopback/Tunnel 제외 NIC의 IPv4 목록 (APIPA, /32 제외), 서브넷 브로드캐스트 주소와 게이트웨이 포함
- `IWakeOnLanService`
  - 102바이트 패킷 생성
  - 대상 IP와 같은 서브넷의 NIC만, 없으면 모든 NIC 선택
  - NIC 로컬 IP에 바인딩한 UDP 소켓으로 서브넷 브로드캐스트 주소 9번 포트에 100ms 간격 3회 전송
- 개별 깨우기: 행 버튼 → 목록 ViewModel 커맨드 (행 템플릿에서 UserControl까지 올라가 호출)
- 전체 깨우기: 대상 체크 + 켜짐이 아닌 PC 전부, 상태를 깨우는 중으로 바꾸고 요청 시각 기록
- 네트워크 일치 확인: 깨울 PC 중 IP가 있는 PC의 대역이 마스터 PC의 어느 NIC(유선/Wi-Fi 구분 없음) 대역과도 맞지 않으면 경고 대화상자와 로그, 전송은 계속
- 전송 결과와 실패를 로그로 남김

완료 기준: 개별/전체 깨우기 시 로그에 전송 기록이 남고, 대상 체크를 해제한 PC는 전체 깨우기에서 빠진다. 다른 네트워크에 연결된 상태에서 깨우면 경고가 뜬다.

## 6단계: 주기적 상태 확인

- `IStatusMonitorService`: 5초 주기, 이전 확인이 진행 중이면 건너뜀, PC별 Ping 1초 타임아웃 병렬 실행
- 판정
  - IP 없음 → 30초마다 백그라운드 ARP 스캔으로 MAC을 찾아 IP를 채우고 켜짐, 못 찾으면 응답 없음 (찾기 전에는 알 수 없음)
  - Ping 성공 → 켜짐 (깨우는 중에서 바뀌면 "켜짐 확인" 로그)
  - 응답 없음 + 깨우기 요청 후 3분 이내 → 깨우는 중
  - 응답 없음 + 3분 경과 → 꺼짐("응답 없음"), 로그에 "BIOS/NIC 설정 확인" 안내 (한 번만)
- 결과는 UI 스레드에서 반영
- "상태 새로고침" 버튼은 즉시 한 번 확인
- 앱 종료 시 확인 중지

완료 기준: 켜진 PC가 켜짐으로, IP 없는 PC가 알 수 없음으로 표시되고, 깨우기 후 3분 동안 깨우는 중이 유지된다.

## 7단계: 서브넷 감지, ARP 스캔, 스캔 영역, 결과 병합

- 스캔 대역 목록: 5단계 NIC 서비스 사용. 기본 선택은 등록된 PC가 속한 대역 → 게이트웨이가 있는 대역 → 첫 번째 대역
- `IpHlpApi.SendARP` P/Invoke, srcIp에 선택 NIC의 로컬 IP 지정
- `IArpScanService`
  - 스캔 범위 계산, 마스크가 /22보다 넓으면 마스터 PC IP의 /24만
  - 동시 32개 제한, `IProgress<T>`로 완료 수 / 전체 수 보고
  - 마스터 PC 자신과 게이트웨이 제외
  - 역방향 DNS 1.5초 타임아웃, 실패 시 IP를 이름으로, 도메인 접미사 제거
- 저장소 병합
  - MAC 일치 → IP 갱신 (IP 충돌 시 다른 항목 IP 비움)
  - MAC 불일치 + IP 일치 → MAC 갱신
  - 둘 다 없음 → 신규, 이름은 호스트명, 대상 체크 꺼짐
  - 응답한 PC는 켜짐으로 설정, 병합 직후 저장
  - 변경마다 로그, 끝나면 "n대 응답 (신규 a, 갱신 b)" 요약
- `ScanViewModel` + `ScanView`: 대역 선택, 스캔 버튼, 진행률, 결과 문구, 첫 스캔 안내. 스캔 중 버튼과 대역 선택 비활성화

완료 기준: 스캔 결과가 목록에 등록되고, 같은 대역을 두 번 스캔해도 중복 등록이 없다.

## 단계 공통 확인

CLAUDE.md의 "변경 후 확인 절차"를 매 단계 해당 범위만큼 확인한다. 특히 경고 0개 빌드와 코드비하인드에 로직이 없는지는 모든 단계에서 확인한다.
