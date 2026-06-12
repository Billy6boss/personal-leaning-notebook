# 大型語言模型 (Large Language Model, LLM)

> **標籤:** `AI` `NLP` `機器學習`  
> **最後更新:** 2026-06-12

---

## 定義

大型語言模型（LLM）是一種以**Transformer 架構**為基礎、透過海量文本資料訓練而成的深度學習模型，能夠理解並生成自然語言。

---

## 核心概念

### Transformer 架構

- 由 Vaswani et al.（2017）於論文 *Attention Is All You Need* 提出
- 核心機制：**自注意力機制 (Self-Attention)**，讓模型能同時關注序列中所有位置的資訊
- 相較於 RNN/LSTM，可平行運算，訓練效率更高

### 預訓練與微調 (Pre-training & Fine-tuning)

| 階段 | 說明 |
|------|------|
| 預訓練 | 在大量無標籤文本上學習語言模式（如 GPT、BERT） |
| 微調 | 針對特定任務（分類、摘要、問答等）以標籤資料進一步訓練 |
| RLHF | 透過人類回饋強化學習，使模型輸出更符合人類偏好 |

### 主要應用

- 文本生成、摘要、翻譯
- 程式碼輔助（GitHub Copilot）
- 問答系統、聊天機器人
- 資訊擷取與分類

---

## 重要模型一覽

| 模型 | 機構 | 發布年份 | 特色 |
|------|------|----------|------|
| GPT-4 | OpenAI | 2023 | 多模態、強推理 |
| Gemini | Google DeepMind | 2023 | 原生多模態 |
| Claude | Anthropic | 2023 | 長上下文、安全導向 |
| LLaMA | Meta | 2023 | 開源 |

---

## 延伸閱讀

- [Attention Is All You Need (Vaswani et al., 2017)](https://arxiv.org/abs/1706.03762)
- [Language Models are Few-Shot Learners (GPT-3)](https://arxiv.org/abs/2005.14165)
