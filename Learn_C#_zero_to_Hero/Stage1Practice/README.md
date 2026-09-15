# Stage 1 練習：Learn C# Properly

[原始參考文件](https://medium.com/net-tips/the-complete-net-developer-roadmap-from-beginner-to-senior-c7bf7559e75d)
對應 `Learn_C#_zero_to_Hero/Stage1.md`（The Complete .NET Developer Roadmap — Stage 1）列出的 11 個子主題，每個主題都有一份可編譯、可執行測試的練習。

## 專案結構
```
Stage1Practice/
├── Stage1Practice.sln
├── src/Stage1.Exercises/            <- 你要實作的地方（每題都有 TODO / throw new NotImplementedException()）
│   ├── Ex01_ControlFlow/
│   ├── Ex02_TypesAndMembers/
│   ├── Ex03_ValueVsReferenceTypes/
│   ├── Ex04_ExceptionHandling/
│   ├── Ex05_Collections/
│   ├── Ex06_Generics/
│   ├── Ex07_DelegatesAndEvents/
│   ├── Ex08_Linq/
│   ├── Ex09_PatternMatching/
│   ├── Ex10_NullableReferenceTypes/
│   └── Ex11_AsyncAwait/
└── tests/Stage1.Exercises.Tests/     <- 檢核用的測試，請勿修改
    └── (與上面資料夾一一對應)
```

## 怎麼練習
1. 打開 `src/Stage1.Exercises` 底下對應主題的 `.cs` 檔，讀懂檔案頂端的 XML 註解（需求規格）。
2. 把 `throw new NotImplementedException();` 換成你的實作。
3. 執行：
   - 全部測試：`dotnet test`
   - 只測某一題：`dotnet test --filter "FullyQualifiedName~Ex05"`（把 Ex05 換成你要測的題號）
4. 全部通過後，可以跟我說「我寫完 ExXX 了」，我會實際編譯/執行測試幫你確認，並給回饋（不會事先告訴答案）。

## 目前狀態
專案現在可以「乾淨編譯」（`dotnet build` 0 error），但幾乎所有測試都會因為 `NotImplementedException` 而失敗 —— 這是正常的起始狀態（TDD 的紅燈），不是 bug。

## 11 個子主題對照表

| # | Stage1.md 主題 | 練習檔案 | 重點 |
|---|---|---|---|
| Ex01 | 變數、資料型別、運算子、流程控制 | `Ex01_ControlFlow/TriangleClassifier.cs` | if/switch、運算子、邊界條件 |
| Ex02 | 方法、類別、record、struct、enum | `Ex02_TypesAndMembers/{MembershipLevel,Money,LoyaltyAccount}.cs` | 何時用 class / record / struct |
| Ex03 | 實質型別 vs 參考型別 | `Ex03_ValueVsReferenceTypes/MutationDemo.cs` | stack/heap、複製 vs 共用參考 |
| Ex04 | 例外處理 | `Ex04_ExceptionHandling/BankAccount.cs` | 自訂例外、try/catch、何時該丟哪種例外 |
| Ex05 | 集合 | `Ex05_Collections/TextAnalyzer.cs` | Dictionary、Stack、HashSet |
| Ex06 | 泛型 | `Ex06_Generics/Repository.cs` | 泛型類別、型別限制式 (constraint) |
| Ex07 | 委派與事件 | `Ex07_DelegatesAndEvents/{RetryHelper,Thermostat}.cs` | Func/Action、event/EventArgs |
| Ex08 | LINQ | `Ex08_Linq/ProductQueries.cs` | Where/Select/GroupBy/OrderBy |
| Ex09 | Pattern Matching | `Ex09_PatternMatching/{ShapeCalculator,TemperatureClassifier}.cs` | switch 運算式、型別/關係/邏輯 pattern |
| Ex10 | Nullable Reference Types | `Ex10_NullableReferenceTypes/{UserProfile,UserProfileHelper}.cs` | `string` vs `string?`、null 安全處理 |
| Ex11 | async/await | `Ex11_AsyncAwait/QuoteService.cs` | Task.WhenAll、取消、避免阻塞式 async |

## 規則
- 請不要修改 `tests/` 底下的檔案，那是檢核標準。
- Ex02 的 `Money`：檔案裡故意先給 `class`，請自行判斷這是不是最適合的選擇。
- 有任何題目卡住，可以直接問我概念（我會用引導的方式帶你思考，而不是直接給答案）。
