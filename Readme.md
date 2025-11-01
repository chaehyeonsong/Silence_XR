# Rules
## 1. Unity settting
1. binary 파일 생성 최소화

    Project setting -> Editor
    - Version Control: Visible Meta Files
    - Asset Serialization: Force Text

2. Scene 작업시 독립된 하위 scene 만들어서 작업 (main scene 수정금지)
    ```bash
        Assets/Scenes/
        |___ Level_Main.unity        # PM
        |___ Level_Background.unity  # background
        |___ Level_Monster.unity       # 괴물 배치
        |___ Level_Lighting.unity    # 라이팅/포스트
        |___ Level_Game.unity        # 미션

    ```

3. 파일 생성시 개인 이니셜 prefix로 설정하기

    ```bash
    ## 예시 c가 prefix 이니셜인 경우
    /prefabs/door (x)
    /prefabs/c_door (o)
    ```

## 2. Git
* scene마다 독립된 branch에서 작업
* main scene은 commit 불가능 (branch merge로만 업데이트 가능)

### 2-1. git 기본 사용법
* 특정 branch clone받기 
(아래는 ssh version, token사용이 편하신 분들은 맨 끝 domain을 https로 바꿔주세요)
```bach
    git clone -b <branch명> git@github.com:chaehyeonsong/Silence_XR.git
    ## 예시
    git clone -b game git@github.com:chaehyeonsong/Silence_XR.git
```
* branch 변경 및 확인
```bash
    # 현재 브랜치 확인
    git branch

    # 브랜치 변경 (여러분들은 딱히 사용할일 없을꺼에요)
    git checkout <이동할 브랜치>

    # 브랜치 생성
    git checkout -b <생성할 브랜치명>
```
* commit
```bash
    # 어떤 파일들이 commit 되는지 확인
    git status

    # 이중 tracking 할 파일 추가
    git add <파일명>
    # or 모두 tracking에 추가
    git add .

    # commit 하기 (commit하기전 자기 브랜치 한번더 확인해보기)
    git branch
    git commit -m "write detail commit message"
```
* push
```bash
    git push origin <branch명>
    ## 예시
    git push origin game
```

### 2-2. Merge settting
```bash
git config --global merge.unityyamlmerge.name "Unity SmartMerge"
git config --global merge.unityyamlmerge.driver '"/PATH/TO/UnityYAMLMerge" merge -p %O %B %A %A'
git config --global merge.conflictstyle merge
```