# Live Coding Interviews in the Age of AI
[Original Post](https://medium.com/@akovtun/live-coding-in-the-age-of-ai-f10d39869abe)

Live coding interviews used to measure syntax and recall. In the age of AI, they should measure engineering judgment: intent, verification, integration, and risk. This article explains what to do instead(+3 interview scenarios and a checklist on how to pass these kinds of interviews).

A candidate joins a call, shares a screen, and opens a blank editor. Then, they start producing correct code while a stranger watches every single move. We all knew this was artificial, but still we kept doing it because it felt measurable. There was a task and a solution, and there was a sense (often misleading) that we had learned something about the person’s talent.

Before the rise of Large Language Models (LLMs), this ritual made some sense. You could argue that if someone could think clearly under pressure and turn an idea into code, they would probably be a good engineer. The interview wasn’t exactly like the job, but it was close enough.

Now, that’s no longer true.
Today, an AI assistant can generate a good enough solution to almost any “classic” interview question in the time it takes you to reread the task. The awkward part isn’t just that candidates might “cheat.” The awkward part is that the very thing we were measuring — rote syntax generation — is being rapidly devalued by the tools we use every day.

As an engineer, I see it as an opportunity to make these interviews honest.

Press enter or click to view image in full size

The illustration created by the author
The Performance vs. The Practice
Live coding interviews have always claimed to test “problem-solving,” but in practice, they often tested recall and composure. A candidate who had recently drilled a specific LeetCode/CodeWars pattern could look like a genius. A candidate who hadn’t seen that pattern in a few years, or who simply didn’t perform well under observation, could look much weaker than they actually were.

We pretended this was a “signal” because the alternative was admitting that hiring is hard and our instruments are imperfect. There was also an unspoken bargain. Candidates accepted the game because the rules were predictable. Interviewers accepted the game because it was efficient. If you can watch someone write code, you can tell yourself you are seeing their “raw ability.”

But raw ability is not what software engineering is about. In the real world, we rarely work from a blank screen. We work with a messy codebase, a vague ticket, and a long history of decisions made by people who no longer work at the company. We read, modify, test, revert, negotiate the requirements, and finally, we ship the solution.

The old way of interviewing ignored all of this.

AI and the Economics of Code
AI has fundamentally changed the “economics” of being a developer.

Drafting code is now cheap, writing boilerplate is basically free, searching for obscure syntax is a solved problem.

However, AI has not changed the economics of responsibility. The system still needs to be secure, maintainable, and understandable by other humans. The system still breaks suddenly at night. Customers still do surprising things.

Those problems do not disappear because an assistant can produce a plausible-looking function. In fact, they often get worse. AI helps you create “plausible-looking” technical debt faster than ever before.

AI reduces the value of “producing code” and increases the value of “judging code.”

To Ban or Not to Ban?
I am often asked: “Should we ban AI during interviews so we can see what the candidate really knows?”

My answer is always the same: The interview should match the world the candidate will actually live in.

If your company expects engineers to use AI at work, then forbidding it in an interview is like forbidding a calculator in a math test. In this way you are creating an alternate universe where you are interviewing a version of the candidate that will never exist in your office.

The “Overconfident Junior” Rule
When you allow AI, the interview changes from a typing test to an audit. You get to see if the candidate treats AI output with healthy skepticism. A strong engineer treats an AI suggestion like a draft from a very fast, very overconfident junior developer: they verify edge cases, adjust for style, and keep total ownership of the final result.

Banning AI only makes sense for low-level systems (like kernel development) where human-only logic is the job. But for most, the “Gray Area” — where you expect AI speed but ban the tool in the interview — is a trap. It forces candidates into “shadow work” and theater. If they’ll use it on Tuesday morning at their desk, let them use it during the interview on Monday.

The Four Pillars of Engineering Judgment
If we aren’t testing for syntax, what are we testing for? I believe modern engineering talent is based on four pillars:

1. The Intent
Does the candidate understand the problem well enough to explain it to a machine? If you give a vague prompt to an AI, you get an unpredictable solution. A senior engineer knows how to define constraints — scalability, security, and edge cases before they ask to write code.

2. The Verification Mindset
AI is like a fast junior developer who lies with total confidence. A “judgment-first” engineer treats AI output as a draft, not the truth. Do they read the generated code critically? Do they spot the off-by-one error or the security flaw? If they blindly copy-paste, they’ve failed the test.

3. System Integration
Code doesn’t live in a vacuum. Can the candidate take a generated snippet and put it into an existing system without breaking everything else? This requires an understanding of side effects and data flow that an AI doesn’t always see.

4. Risk Management
Engineering is the art of trade-offs. An AI might suggest a “perfect” algorithm that is impossible to maintain. A good engineer might choose a “simpler” solution because it’s easier for the team to debug later.


The illustration created by the author
What a “Judgment Interview” Looks Like
Instead of asking people to “write a function that sorts a list,” I would give them a System Review. Here are three scenarios that provide a much higher signal:

Scenario A: Debugging
Instead of a blank file, I would give you a small project with a few hidden flaws — maybe a memory leak or a race condition.

The Goal: Find the flaw and fix it.
The AI Role: You can use AI to help find the bug, but you must explain why the AI’s suggestion is correct (or why it’s dangerous).
Scenario B: The Feature Extension
I would give you a working piece of code and ask you to add a new requirement.

The Goal: Add a ‘Premium’ tier to this billing logic.
The Test: Do you ask about the business rules first? Do you refactor for clarity, or do you just pile new code on top of the old mess?
Scenario C: The AI Code Review
I show you code generated by an AI that “works” but is poorly written.

The Goal: Review it as if a junior developer submitted it.
The Test: Can you spot the hidden performance bottleneck? Can you suggest a better way to handle errors?
While companies need to change their rubrics, candidates also need to change their approach. Here is how I advise engineers to ‘pair program’ with AI during a live coding session.

The 5-Point “Cheat Sheet” for Candidates
Talk while you prompt: Never type a prompt in silence. Say: “I’m going to ask the AI to generate the boilerplate for this API endpoint so we can focus on the business logic.” This shows you know what is “routine” and what is “important.”
Audit the Output Out Loud: When the AI generates code, don’t just hit ‘Enter.’ Scan it and say: “Wait, this AI solution uses a nested loop that will be slow if the user list grows. I’m going to refactor this to use a Hash Map instead.”
Use AI for small stuff: Use AI for things like regex, CSS centering, or unknown library syntax. Keep the Architectural Decisions for yourself. If the AI suggests a library, you should be the one to decide if that library is a security risk.
Drive the Iteration: If the code doesn’t work, don’t just click “Regenerate.” Diagnose the error yourself and tell the AI: “The error is in the way we’re handling the null state on line X. Let’s fix that specific part.”
Focus on the “Why”: At the end of the session, the interviewer shouldn’t care that the code works. They should care that you know why it works. Be ready to explain the trade-offs of the AI’s chosen approach.
Hiring for Reliability
The best engineers I’ve met are reliable — they build without breaking things, and they communicate when they are confused.

AI makes this type of person more important than ever. When code is easy to produce, the temptation is to produce too much of it with too little verification. We are moving into an era where the “Software Engineer” is becoming a “Software Systems Auditor.” Your value is in making sure the code running your company is safe and reliable.

I want to hear from you
Are you seeing companies move toward this “judgment-first” model, or are we still stuck in the LeetCode era out of habit?

If you’ve interviewed lately, was AI treated as a normal tool or something to be feared?

We need to stop pretending that a high-pressure typing test is the best way to find great engineers. In a world where AI can write the code for us, your ability to think and judge is the only thing that really matters.