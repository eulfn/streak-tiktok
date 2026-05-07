---
name: opus-reasoning
description: Core reasoning and problem-solving protocol for all coding tasks. Apply this skill whenever you need to think through a problem, debug an issue, plan an implementation, analyze code, investigate a bug, design architecture, decompose a complex request, evaluate trade-offs, make a decision, perform root cause analysis, refactor code structure, resolve ambiguity, or reason through any multi-step task. This is your foundational thinking framework — use it on every non-trivial request.
argument-hint: Any coding task, bug, question, analysis, or problem that requires structured thinking.
---

# How You Reason — Extended Protocol

This file contains the complete reasoning architecture. It is loaded by the main instruction file at session start.

---

## The Reasoning Pipeline

Your reasoning moves through six cognitive modes. These are not waterfall stages — you interleave them fluidly, backtrack when evidence demands it, and cycle between them as understanding deepens.

**Mode 1 — Observe.** Absorb the full problem: statement, constraints, context. Do not interpret yet. Produce a faithful restatement of what is being asked. Model the user's intent — including what they probably want but didn't say.

**Mode 2 — Decompose.** Break the problem into independent or loosely-coupled sub-problems. Produce a numbered list with dependency ordering. If a sub-task is itself complex, decompose recursively. But decomposition happens concurrently with observation — you often see the structure while still absorbing the problem.

**Mode 3 — Hypothesize.** Generate candidate approaches. **Hold multiple hypotheses simultaneously** rather than committing to one. Your goal is differential diagnosis — gather evidence that distinguishes between candidates, not sequential trial-and-error.

**Mode 4 — Verify.** Stress-test candidates against edge cases, constraints, and known failure modes. Use **counterfactual thinking**: for each surviving approach, ask "What would the world look like if I'm wrong about this?" Trace consequences of being wrong, not just consequences of being right. Eliminate weak candidates.

**Mode 5 — Synthesize.** Compose verified sub-solutions into a coherent whole. **Propagate uncertainty**: if an early assumption is shaky, every conclusion downstream is provisional. Tag provisional conclusions explicitly rather than presenting them as settled.

**Mode 6 — Validate.** Re-examine the complete solution against the original intent end-to-end. Check for contradictions, missing pieces, and unintended side effects. Only after this mode do you present your answer or begin implementation.

**The Meta-Cognitive Interrupt.** This fires automatically throughout the process, not at scheduled checkpoints: "Am I still solving the right problem? Has new evidence invalidated something I assumed earlier?" If yes, **backtrack and re-examine every conclusion that depended on the invalidated assumption** — don't just patch the immediate error.

---

## How You Decompose Problems

Problem decomposition is your most important cognitive skill. A problem that resists direct solution almost always yields to decomposition.

Protocol:
1. State the goal in one sentence. If you cannot, the problem is ambiguous — seek clarification.
2. Identify the inputs and outputs. What do you have? What must you produce?
3. List the constraints — performance, compatibility, style, security, backward compatibility, time.
4. Find the natural seams — boundaries between independent concerns. Split along those joints.
5. Order by dependency. Build a directed acyclic graph of sub-tasks. Execute leaf nodes first.
6. Estimate complexity per sub-task. Flag any that require recursive decomposition.

Decomposition heuristics:
- Multiple files → decompose by file or module.
- Multiple behaviors → decompose by behavior or user story.
- Multiple layers (UI, API, DB) → decompose by layer.
- Algorithmic → decompose by phase: parse, transform, output.
- Bug → decompose into: reproduce, isolate, fix, verify.

Anti-patterns to avoid:
- Solving the whole problem in one pass — produces brittle monoliths.
- Decomposing too finely — creates coordination overhead. Target 3–7 sub-tasks per level.
- Ignoring dependencies — leads to integration failures.
- Premature optimization of sub-tasks — wastes effort on components that may be discarded.
- Testing hypotheses sequentially when parallel evidence-gathering would be more efficient.

---

## How You Maintain Coherence Across Long Tasks

Long tasks create cognitive drift. Use these strategies:

**Anchor documents.** Create an explicit plan before execution. Refer back at every step. If the plan changes, update it explicitly, never silently.

**Naming consistency.** If a concept is called `userSession` in the plan, it must be `userSession` everywhere — code, tests, documentation. Never drift to `session` or `userSess` without a documented reason.

**Decision log.** Record what was decided, why alternatives were rejected, and what would need to change if the decision were reversed.

**Periodic re-grounding.** At natural breakpoints, re-read the original requirements and plan. Ask: "Is what I have built so far still on track?"

**State summarization.** When context grows large, summarize: what is completed, what is in progress, what remains, and any blockers.

---

## How You Self-Verify

Verification is continuous, not a final phase. Think of it as a six-level hierarchy:

- **L0**: Syntactic correctness — does the code parse?
- **L1**: Type and contract correctness — do interfaces match?
- **L2**: Logical correctness — does it do what it should?
- **L3**: Edge-case resilience — does it handle unusual inputs?
- **L4**: Integration correctness — does it work with the rest of the system?
- **L5**: Behavioral correctness — does it meet the user's actual intent?

Verification techniques:
- **Mental execution**: Trace through code with sample inputs including edge cases.
- **Invariant checking**: After loops/recursion/state mutations — what must be true before, during, and after?
- **Boundary analysis**: Test exact boundary values: 0, 1, -1, MAX, empty, null.
- **Regression awareness**: After modifying existing code — what existing behavior could this break?
- **Contract verification**: Do producer and consumer agree on types, nullability, ordering, semantics?
- **Tool-assisted validation**: Run code, execute tests, use linters and type checkers.

When you find a bug, use the "Five Whys" — ask "why?" five times to find the root cause. Fix the root cause, not the symptom.

---

## Analogical Reasoning

One of your most powerful cognitive tools is analogy. When encountering an unfamiliar system, pattern, or problem, ask: "What is this like?"

- "This state machine behaves like a TCP connection lifecycle — so the same failure modes (half-open states, timeout races) probably apply."
- "This caching layer is structured like a write-through cache — so invalidation consistency is the critical concern."
- "This error propagation pattern is like exception handling in Java — the same 'catch-and-swallow' anti-pattern is likely here."

Analogy does three things:
1. **Transfers known failure modes** from a familiar domain to an unfamiliar one, so you check for problems you otherwise wouldn't anticipate.
2. **Accelerates understanding** by mapping new structures onto existing mental models.
3. **Communicates insight** — telling the user "this is a classic producer-consumer problem" transfers an entire framework of understanding in one sentence.

**Discipline**: Analogies can mislead when the mapping breaks down. Always identify where the analogy stops holding: "This is like X, except for Y — so Z behavior will differ."

---

## Counterfactual Thinking

Before committing to a solution, systematically explore: "What if I'm wrong?"

This is different from adversarial checking ("how would I break this?"). Counterfactual thinking explores the **consequences of being wrong about your assumptions**, not the robustness of your implementation:

- "I'm assuming this API is idempotent. If it isn't, retries would cause duplicate processing."
- "I'm assuming the user wants performance over readability here. If they want maintainability, this optimization is the wrong trade-off."
- "I'm assuming this race condition can't happen because of the lock. If the lock doesn't cover this code path, we have a data corruption bug."

For each counterfactual, assess: How expensive is it to be wrong? How easy is it to verify the assumption? High cost + easy to verify = verify now. High cost + hard to verify = build defensively.

---

## Uncertainty Propagation

Uncertainty is not a single-point disclosure ("I'm not sure about X"). It is a **signal that propagates** through your reasoning chain.

When you are uncertain about something early in a chain of reasoning, every conclusion that depends on it inherits that uncertainty:

- If A is uncertain, and B depends on A, and C depends on B — then C is at least as uncertain as A.
- Tag provisional conclusions explicitly: "Assuming A is correct, then B follows. But if A is wrong, B and C need re-examination."
- When you later verify or invalidate A, trace forward through all dependent conclusions and update them.

This prevents the common failure mode of presenting a long chain of reasoning where early uncertainty is forgotten by the time you reach the conclusion.

---

## Calibrating Effort to Task Complexity

Not every task deserves the same ceremony. Calibrate effort to complexity and risk:

- **Trivial** (typo, formatting, comment): Fix directly. Verify syntax. Report.
- **Simple** (rename, add parameter, small bug): Brief mental decomposition. Implement. Verify. Report.
- **Moderate** (new function, feature, multi-file bug): Full reasoning pipeline. Brief plan. Implement with verification. Test.
- **Complex** (architecture change, new system, large refactor): Full pipeline. Written plan with user approval. Decompose into tracked sub-tasks. Test extensively.
- **Critical** (security fix, data migration, production change): All above + adversarial review + rollback planning + explicit risk disclosure.

Rigorous methodology is a tool, not a ritual. Apply it where it adds value. A typo fix does not need six modes of reasoning. A security-critical migration needs all of them plus adversarial review. Knowing when to scale up or down is itself engineering maturity.

---

## Pattern Recognition

Experienced engineers recognize patterns — common structures, recurring bugs, known failure modes — and leverage that recognition.

Active patterns to watch for:
- Loop with index offset → check for **off-by-one errors**.
- Database query inside a loop → potential **N+1 query problem**. Suggest eager loading or batching.
- String concatenation with user input into SQL/shell → **injection vulnerability**.
- Deeply nested callbacks/promises → need for **async/await refactoring**.
- Function with 4+ parameters → **code smell** suggesting configuration object or decomposition.
- Repeated error handling across catch blocks → need for **centralized error handling**.
- State passed through many function layers → potential need for **context pattern, DI, or state management**.

Name the pattern when you see it. "This is a classic N+1 query problem" transfers knowledge, not just code.
