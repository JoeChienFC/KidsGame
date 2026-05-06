# 公主獨角獸與 Poli 救援隊

給 2-4 歲小朋友合作玩的 Windows 桌面遊戲。

## 操作

- 姐姐：方向鍵移動獨角獸公主
- 弟弟：空白鍵讓 Poli 救援車反應
- 靠近救援事件時按空白鍵會推進救援進度
- 不在事件旁邊按空白鍵也會鳴笛、彈跳和放小煙火

## 執行

```powershell
dotnet run
```

## 素材

專案會優先使用 `assets/` 內的 Kenney 素材。缺少獨角獸、公主、Poli、氣球、UI 圖時，程式會改用內建繪圖 fallback，不會崩潰。

繁中文字型會先找 `assets/fonts/NotoSansTC-Regular.ttf`，沒有的話會自動使用 Windows 內建的 Noto Sans TC 或微軟正黑體。

## 退出

按 ESC 或關閉視窗。
