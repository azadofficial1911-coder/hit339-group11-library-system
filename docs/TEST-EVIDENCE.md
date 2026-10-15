# R2 and R4 test evidence

Executed on 15 October 2026 in this Linux development environment using .NET SDK 10.0.100, .NET/ASP.NET runtime 10.0.0, EF Core 10.0.10 and an isolated SQLite database per test. The live application's database provider remains SQL Server. SQLite tests exercise relational constraints and transactions through the real MVC/Identity pipeline; they do not establish SQL Server lock/deadlock behaviour or LocalDB/VM installation.

## Results obtained

| Check | Actual outcome | Evidence |
| --- | --- | --- |
| Release solution build | Passed; zero warnings and errors | `evidence/release-build.txt` |
| Automated Release integration suite | 36 passed, 0 failed, 0 skipped | `evidence/automated-tests.txt`, `evidence/azd-r2-r4-results.xml` |
| SQL Server model versus migration snapshot | No pending changes | `Sql_server_migrations_match_model_and_generate_script` test and EF CLI |
| SQL Server idempotent SQL generation | Passed; branches, desks and transfers included | Same test; EF CLI script generation |
| Dependency restore after package pin updates | Passed without vulnerability warnings | Final build/test output |

The original dependency graph reported Pkcs security advisories. A direct reference to patched 10.0.13 removes those warnings. The test-only SQLite native bundle is pinned to 3.0.5 to avoid the old vulnerable native dependency. References: https://github.com/advisories/GHSA-846f-f764-76mx and https://github.com/advisories/GHSA-2fmv-rv2c-59x7. Both identify Pkcs 10.0.13 as patched.

## Behaviour exercised

- Every API endpoint returns 401 for missing/wrong keys; unset API configuration stays closed. A valid key enables the three GET routes. Staff login cookies alone do not grant API access.
- POST, PUT, PATCH and DELETE return 405 on each API route. Invalid branch/page parameters return 400. Public API projections omit patron data and credentials.
- Available stock appears at its source branch, disappears from both locations on dispatch and appears at the destination only after receipt. The item count remains one.
- Duplicate dispatch/receipt, receipt at the source, missing/inactive destinations, same-branch moves, unassigned copies and unavailable statuses are refused.
- Active loans and holds block transfer and API availability even when the item has a stale Available status.
- A stale item update after dispatch throws an EF concurrency exception. The reservation service cannot release transit stock. A hold created during transit waits and becomes Ready only at receipt, with a simulated notification log.
- Reception dispatch and receipt work through real antiforgery-protected HTTP forms. Borrowing and manual Book editing cannot overwrite transit state. Admin pages and baseline Book/Music/Toy/Catalogue pages render successfully.
- Anonymous and Manager users cannot manage transfers. Reception cannot manage branches/desks. Missing antiforgery tokens are rejected.
- Branch/desk forms accept valid input and reject duplicates/missing fields. Branch codes normalise to uppercase; an active desk prevents branch deactivation.
- Repeating the optional three-branch seed does not duplicate branches, desks or demo copies.

## Checks still required before final push/merge readiness

| Check | Status | Responsible evidence |
| --- | --- | --- |
| Run the suite with `AZD_TEST_SQLSERVER` against Windows LocalDB/SQL Server | Not run here; no SQL Server instance available | Azd; commands in installation guide |
| Upgrade the existing laptop database with both added migrations | Not run here | Azd; migration output and working pages |
| Fresh VM clone, database setup, startup and two branch selections | Not run here | Malia, following Azd's guide |
| Visual keyboard, NVDA, contrast, text resize and narrow-screen checks | Not run here; labelled controls and semantic markup are implemented | Shared accessibility reviewers |
| R2 peer review | Pending | Asraful; genuine review/PR link |
| R4 peer review | Pending | Safiyan; genuine review/PR link |
| Reconcile against current remote main | Pending; only uploaded commit 39a5cac and its local changes were available | Compare/review before PR merge |

No GitHub issue numbers, review approvals, actual work hours or VM screenshots have been invented. The repository's existing Assignment 2 README describes its earlier testing; it is not evidence of new R2/R4 testing.

Implementation and test preparation used ChatGPT/Codex assistance. Git commits use Azad's supplied name/email; that attribution does not certify independent authorship or lecturer approval. Review the implementation and follow the unit's disclosure requirements.
