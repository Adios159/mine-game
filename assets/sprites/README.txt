시트 규칙 (전부 4배 확대 도트 -> 텍스처 필터 Nearest)
player_sheet.png      64x64 x 3열 3행. 1번 칸=대기, 걷기=2~7번(6프레임, 100ms), 8~9번 빈칸
rock_shiny_sheet.png  64x64 x 3열 3행. 채워진 칸 7개(gif는 10프레임, 확인 필요). 돌덩이 자체에 쓰면 규칙 위반
drill_sheet.png       64x64 x 3열 2행, 6프레임, 70ms
ore_{key}_sheet.png   32x32 x 6열 1행, 6프레임, 100ms
map_full.png          1920x1080. 위 896px이 맵(30x14칸), 아래 184px은 검은 띠
ui/ore_list/list_*.png  224x32 한 장 = 한 줄(list_diamond만 64x32, 게이지 없음). 화면에서 0.25배(56x8)
ui/ore_list 파일 번호 = 현재 개수 + 1 (list_gold_1 = 0개). 파일 수 = GameData.UnlockNeed, 개수 >= Need면 해금 아이콘(ui/unlock/unlock_{key}_{효과}.png, 64x64)
광석 파일 키 key = GameData.OreKeys (diamond, emerald, gold, iron, coal). 전부 소문자
