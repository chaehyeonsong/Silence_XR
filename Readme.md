# Rules
## Unity settting
1. binary 파일 생성 최소화

    Project setting -> Editor
    - Version Control: Visible Meta Files
    - Asset Serialization: Force Text

2. Scene 작업시 독립된 하위 scene 만들어서 작업 (main scene 수정금지)
    ```bash
    Assets/Scenes/
    |___Level_Main.unity        # PM
    |___Level_Background.unity  # background
    |___Level_Ghost.unity       # 괴물 배치
    |___Level_Lighting.unity    # 라이팅/포스트
    |___Level_Game.unity        # 미션

    ```

3. 파일 생성시 개인 이니셜 prefix로 설정하기

    ```bash
    ## 예시 c가 prefix 이니셜인 경우
    /prefabs/door (x)
    /prefabs/c_door (o)
    ```

## Git
* scene마다 독립된 branch에서 작업
* main scene은 commit 불가능 (branch merge로만 업데이트 가능)


### Merge settting
```bash
git config --global merge.unityyamlmerge.name "Unity SmartMerge"
git config --global merge.unityyamlmerge.driver '"/PATH/TO/UnityYAMLMerge" merge -p %O %B %A %A'
git config --global merge.conflictstyle merge
```