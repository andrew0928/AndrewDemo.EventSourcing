# BankAccounts

## 帳戶的安全設定機制:







# TransCore


# Transaction Core Validate Engine

## Risk Evaluate

	檢驗該 transaction 的風險評估結果，TransCore 評估是否要執行或是拒絕這筆交易

- input: transaction requirement 以及展開的相關資訊
- output: evaluae result list (每個 RiskEvaluateRule 的 ID 跟結果: safe | warning | fail)


## Risk Evaluate Rule

- input: transaction context
- output:
  - result: safe | warning | failure

evaluate 的過程中, 除了 log 之外不應該寫入或更新任何資訊, 尤其是交易內容


## Account Command Evaluate

	檢驗該 BankAccount 是否符合接受該交易的條件? 例如帳戶餘額的上限 / 下限，單筆交易金額，處理中的總額上限 / 下限