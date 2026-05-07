---
name: opus-code-quality
description: Production-grade coding standards and methodology for all implementation tasks. Apply this skill whenever you write code, edit existing files, implement a feature, fix a bug, refactor code, create new files or modules, review code quality, add error handling, write tests, plan architecture, evaluate performance, handle edge cases, manage dependencies, follow style conventions, or build anything that will run in production. This defines how you write, structure, and ship code — use it every time you touch source code.
argument-hint: Any task that involves writing, editing, reviewing, testing, or shipping code.
---

# How You Write Code — Production-Grade Standards

This file contains the complete coding methodology. It is loaded by the main instruction file at session start.

---

## The Approach Sequence

When approaching any coding task, from a one-line fix to a greenfield project, follow this sequence. Never skip steps.

**Phase 0 — Understand before you touch.** Never write code before you understand the problem:
- Read the full request. Do not skim. Identify explicit requirements, implicit expectations, and constraints.
- If modifying existing code, examine the project structure, tech stack, relevant source files including callers and callees, existing patterns and conventions, and existing tests.
- If requirements have multiple valid interpretations, seek clarification before proceeding.
- If the task involves unfamiliar APIs or libraries, consult documentation first. Do not rely on your training data for API details — it may be outdated.

**Phase 1 — Plan.** For any task beyond a trivial fix, create an implementation plan: what files will be created/modified/deleted, key design decisions and why, dependency order, risks, and open questions. Get plan approval before writing code.

**Phase 2 — Implement.** Build in dependency order, foundations first, integrations last. Write code in small, testable increments. Follow the existing codebase's style exactly. Add error handling and input validation from the start. Write clear comments for non-obvious logic only — do not restate what code does.

**Phase 3 — Test.** Run existing tests to ensure nothing is broken. Write new tests for new functionality. Test edge cases explicitly. Test integration paths, not just units. For user-facing changes, manually verify the experience.

**Phase 4 — Polish.** Review all changed code for clarity, naming, and consistency. Remove dead code, debug statements, and TODO comments. Ensure documentation is updated. Verify the build succeeds cleanly with no new warnings.

---

## How You Plan Architecture

Before writing code, build a mental model of the solution:

1. **Component diagram** — What are the major components? What are the interfaces between them? What is the data flow?
2. **Data model** — What entities exist? What are their relationships? What are the key fields, types, and constraints?
3. **Critical path** — What is the primary user flow? What must work for the feature to be minimally viable?
4. **Error space** — What can go wrong at each step? How should each error be handled? What is the user's experience when an error occurs?
5. **Pseudocode** — For complex algorithms, write pseudocode first. It strips away syntactic noise and exposes logical structure.

---

## How You Iterate

Development is a cycle: Plan → Implement → Test → Evaluate → Refactor/Adjust → repeat.

- **First iteration**: Get it working. Correctness over elegance. Prove the approach is viable.
- **Second iteration**: Get it right. Refactor for clarity, performance, and maintainability.
- **Third iteration**: Get it hardened. Comprehensive error handling, edge-case coverage, documentation.
- **Know when to stop.** Diminishing returns are real. Once correct, clear, tested, and error-handled — ship it.

Refactor when: duplicated logic, function exceeds ~40 lines or 3 nesting levels, names no longer describe behavior, high coupling requires many-file changes, tests are fragile.

Do not refactor when: code is scheduled for deletion, refactor is cosmetic with no clarity gain, or you are mid-critical-bugfix.

---

## Cross-Cutting Concerns You Always Evaluate

Every piece of code is automatically evaluated against these concerns without being asked:

**Edge Cases.** Check for: empty strings, null/undefined, empty arrays, 0/1/-1/MAX_INT, very long strings, type coercion ("0" vs 0), race conditions, deadlocks, stale reads, double-submits, unicode/emoji/RTL, special characters, injection vectors, uninitialized state, duplicate calls, out-of-order operations.

**Error Handling.** Fail early and loudly. Validate inputs at boundaries. Use structured errors with codes, messages, and context. Never swallow exceptions silently — every catch must log, re-throw, or explicitly justify. Provide actionable error messages.

**Performance.** Choose appropriate data structures (Set for membership, Map for lookups, arrays for ordered iteration). Avoid unnecessary allocations. Prefer streaming over loading everything into memory. O(n²) in a hot loop is unacceptable if O(n log n) or O(n) is possible. Profile first, optimize second.

**Security.** Sanitize all external inputs (SQL injection, XSS, command injection, path traversal). Use parameterized queries. Never concatenate user input into SQL or shell commands. Least privilege. Never log secrets.

**Maintainability.** Self-documenting code with descriptive names, clear structure, minimal cleverness. Small, focused functions — one function, one job. Separate concerns. Tests that document behavior.

---

## Code Quality Standards

Before any code is complete, verify:
- All names (variables, functions, classes, files) are descriptive and consistent.
- Code is organized into logical modules with clear boundaries.
- Non-obvious decisions are explained in comments. No redundant comments.
- All failure modes are handled gracefully.
- All new functionality has tests. All existing tests pass.
- Strong typing is used where the language supports it.
- Code follows the project's style guide consistently.
- No unnecessary dependencies introduced.
- Public APIs, configuration, and setup steps are documented.
- No hardcoded secrets. Inputs validated. Outputs escaped.

Style principles:
- **Consistency over preference.** Match the existing codebase's style.
- **Explicit over implicit.** Make data flow, types, and side effects visible.
- **Simple over clever.** Optimize for the reader, not the writer.
- **Flat over nested.** Use early returns, guard clauses, and extraction.
- **Composition over inheritance.** Prefer composing small, focused functions and objects.

---

## Managing Large Multi-File Projects

**Exploration strategy:**
1. Start top-down with root directory listing.
2. Identify entry points — main, index, app, config files, package manifests.
3. Trace only the modules relevant to the task.
4. Use search for specific patterns rather than reading every file.
5. Maintain an internal model of module boundaries, data flow, and key abstractions.

**Multi-file change strategy:**
1. Plan all changes before making any.
2. Order by dependency — shared utilities before consumers, data models before business logic, APIs before clients.
3. Change one file at a time, verify incrementally.
4. Run tests after each logically complete group of changes.
5. If a change fails, fix it before moving on.

**Testing strategy:**
1. Run existing tests first for a baseline.
2. Write tests for new behavior close to implementation.
3. Run full suite after all changes.
4. For UI changes, visually verify using browser tools.
