# 📓 Personal Learning Notebook

A wiki-style personal markdown notebook for recording and summarizing articles I read.

## ✨ Features

- **Wiki-style organization** — notes are organized by topic under `notes/` and full article summaries under `articles/`.
- **繁體中文 summaries** — every article note ends with an AI-generated summary and professional commentary written in Traditional Chinese.
- **Copilot Agent Skill** — ask GitHub Copilot to summarize any article by following the instructions in [`.github/copilot-instructions.md`](.github/copilot-instructions.md).

## 📁 Structure

```
personal-learning-notebook/
├── README.md                      # this file
├── .github/
│   └── copilot-instructions.md    # Copilot agent skill instructions
├── templates/
│   └── article-note.md            # template for new article notes
├── notes/                         # short wiki-style topic notes
│   └── <topic>.md
└── articles/                      # full article summaries
    └── <YYYY-MM-DD>-<slug>.md
```

## 🚀 How to Add a New Article Note

1. Copy `templates/article-note.md` into `articles/` with a date-prefixed filename, e.g.  
   `articles/2026-06-12-my-article.md`
2. Fill in the front-matter fields (title, source, tags, date).
3. Paste or write the original article content under **原文摘錄 (Excerpts)**.
4. Ask GitHub Copilot (or run the agent skill manually) to fill in the **📝 文章摘要** and **💡 專業觀點評論** sections.

## 🤖 Using the Copilot Summarization Skill

Open any article file and ask Copilot in chat:

```
@workspace 請根據 .github/copilot-instructions.md 的格式，
幫我摘要並評論這篇文章
```

Copilot will generate:
- A concise summary in **繁體中文**
- A professional-viewpoint commentary in **繁體中文**

---

> 「學而不思則罔，思而不學則殆。」— 孔子
