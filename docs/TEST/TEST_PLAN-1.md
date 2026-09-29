# Test Plan #1 - Sprint 4 - Reporting Module & Dashboard - 22/09/2026

**Status:** Recorded — completion evidence captured; live E2E follow-up pending

---

## Overview

This plan verifies the Sprint 4 Reporting module and Dashboard end-to-end: the three reporting API endpoints, the Dashboard and Reports pages with Recharts visualizations, TickerQ background jobs, and the database objects that back them. The expected outcome is a full pass over every case below, producing evidence that read-side aggregates are accurate, validated, and kept fresh by cache invalidation and scheduled jobs.

---

## Scope Definition

### What's Included

- Backend reporting endpoints: `GET /api/reports/dashboard/summary`, `GET /api/reports/dashboard/income-vs-expenses`, `GET /api/reports/dashboard/category-breakdown` (authentication, `ApiResponse<T>` envelope contract, parameter validation, UTC date normalization, month-gap filling)
- Dashboard page at `/` - `OverviewCards`, `SpendingPieChart` (current month), `IncomeExpenseChart` (6-month trend)
- Reports page at `/reports` - month picker, selected-month category breakdown, 12-month trend
- Cross-feature cache invalidation: transaction and category mutations refresh dashboard queries
- TickerQ operational store, seeded cron tickers, `MonthlyReportJob` (idempotent monthly summary upsert), `BudgetAlertJob` (80% threshold scan and logging)
- Database objects: `reports.monthly_summaries` (unique `(user_id, year, month)` index), `ticker` schema with TickerQ tables
- Regression: transactions list date filter (end-of-day inclusive semantics, UTC-kind normalization from Task 4)
- Frontend auth guards on `/` and `/reports` (redirect + 401 behavior)
- User data isolation - dashboard and reports only show the authenticated user's data

### Out of Scope

- Writing automated tests (Sprint 5: Vitest + MSW frontend, TestContainers backend integration)
- Email/push notifications for budget alerts (Sprint 4 scope is job logging only)
- Deferred dashboard widgets: `BudgetProgressChart`, `RecentTransactions`
- Persisted-summary read endpoints (`reports.monthly_summaries` is job output only, no read API)
- TickerQ Dashboard UI package (`TickerQ.Dashboard`)
- Auth hardening finding C-1 (JWTs in `localStorage` - deferred by owner decision; not re-tested here)
- Performance and load testing
- Authoring or extending the existing backend unit test suites (they run in CI; this plan only sanity-checks them green)

### Known Gaps

- No browser automation tooling is installed (Playwright/Cypress absent) - every E2E case is a scripted manual execution; automation is a Sprint 5+ candidate
- Job cadences (1st of month, every 6 hours) cannot be observed naturally within a session - verifying the jobs requires a temporary cron override that MUST be reverted before commit
- Pixel-perfect checks are manual (no visual-regression tooling); `docs/ai/ui-design-rules.md` still contains MUI-era guidance that does not match the Tailwind as-built - when in doubt, the running app is the standard
- Because of the deferred C-1 finding, token/session security testing is explicitly excluded - it is tracked, not tested, in this plan

---

## Test Environment

The environment must mirror production as closely as the local setup allows:

1. Database: Docker Desktop PostgreSQL 18 - `cd infrastructure && docker compose up -d` (localhost:5432)
2. Both Reporting migrations applied: `AddMonthlySummariesTable` and `AddTickerQTables`
3. Backend: ASP.NET API at `http://localhost:5194` - Scalar reference at `/scalar`, health at `/health/live` and `/health/ready`
4. Frontend: `task local` - serves the production Vite build at `http://localhost:3000` (closest to production; `task local-debug` is acceptable when iterating, but the final pass must run on the production build)

Pre-flight checklist (all must pass before executing cases):

1. `GET /health/live` and `GET /health/ready` return 200
2. `dotnet test backend/Personal.FinanceTracker.slnx` - 212/212 passing (134 Finance, 59 Users, 19 Reporting)
3. `cd frontend && npm run build-lint` - ESLint, `tsc -b`, Vite build all green

Credentials: register the test users below with throwaway passwords (placeholder: `********`). Never record real credentials in documents or screenshots with visible secrets.

---

## Execution Record — 23/09/2026 Completion-Record Smoke Run

**Evidence reference:** `docs/ai/sprints/sprint-4.md` → **Sprint Completion Record (23/09/2026)** (`CR-2026-09-23`). That record documents the pre-flight builds/tests, database migration and ticker checks, the monthly-summary idempotency smoke, the reporting API smoke, and the manual responsive/accessibility audit.

The per-case statuses below are now explicit and traceable to that completion record. `Passed (CR)` means the completion record directly covers the case's behavior; `Recorded — partial` means only the listed API or manual evidence was captured; `Not evidenced` means the case was not demonstrated in the completion record. These are not claims of a fresh browser run during this audit follow-up: Docker Desktop's Linux daemon was unavailable and neither local service was running, so no new live HTTP or UI evidence could be collected.

| Case | Result | Evidence / limitation |
|---|---|---|
| 1 | Recorded — partial (CR) | API summary smoke returned balance `650`, income `1000`, and expenses `250`; the completion record did not retain the browser card rendering or this plan's hand-seeded values. |
| 2 | Not evidenced (CR) | No two-user runtime comparison was recorded; source endpoints use the authenticated user claim. |
| 3 | Recorded — partial (CR) | API smoke returned three contiguous trend rows with zero-filled gaps; six rendered chart points were not retained. |
| 4 | Recorded — partial (CR) | Category-breakdown API smoke returned `250`; the four-slice browser legend and ordering were not retained. |
| 5 | Not evidenced (CR) | No month-picker interaction or selected-month UI evidence was recorded. |
| 6 | Not evidenced (CR) | No transaction/category mutation followed by a live dashboard refresh was recorded. |
| 7 | Recorded — partial (CR) | Unauthenticated dashboard request returned `401`; all three endpoint calls and both route redirects were not separately recorded. |
| 8 | Recorded — partial (CR) | Enveloped success/trend responses and `months=25` enveloped `400` were recorded; every invalid-input variant in the case was not run. |
| 9 | Not evidenced (CR) | No zero-transaction user run was recorded. |
| 10 | Passed (CR) | Transactions date filtering returned `200` with inclusive end-of-day behavior; the completion record notes no `DateTimeKind` failure. |
| 11 | Not evidenced (CR) | No soft-delete aggregate comparison was recorded. |
| 12 | Not evidenced (CR) | No negative-balance user UI run was recorded. |
| 13 | Passed (CR) | Monthly-summary smoke generated 7 rows, updated 7 on the second run, and left 7 persisted rows. |
| 14 | Not evidenced (CR) | Scheduler activity was recorded, but no retained threshold-scan warning/count evidence was captured. |
| 15 | Passed (CR) | `reports.monthly_summaries`, the three `ticker` tables, required indexes, and both enabled seeded tickers were verified; cron values were recorded in seconds-expanded form. |
| 16 | Recorded — manual audit (CR) | Responsive dashboard audit was recorded; browser-level pixel measurements were unavailable. |
| 17 | Recorded — manual audit (CR) | Dashboard chart responsiveness was manually audited; no visual-regression evidence was retained. |
| 18 | Recorded — manual audit (CR) | Mobile shell collapse and layout intent were manually audited; a browser pixel pass was unavailable. |
| 19 | Not evidenced (CR) | No throttled-network skeleton capture was recorded. |
| 20 | Not evidenced (CR) | No stopped-backend error-state capture was recorded. |
| 21 | Not evidenced (CR) | No complete rendered formatting inspection was recorded. |
| 22 | Passed (CR) | Accessibility attributes and responsive dashboard behavior were included in the manual audit; no screenshot or screen-reader transcript was retained. |
| 23 | Not evidenced (CR) | No keyboard/month-input interaction run was recorded. |
| 24 | Not evidenced (CR) | No browser title/sidebar/deep-link run was recorded. |
| 25 | Recorded — partial (CR) | Mobile interaction and tap-target intent were reviewed as part of the manual audit; measured touch targets were not retained. |

**Required follow-up:** when Docker Desktop is available, run Cases 1–25 against a fresh database in the order below, capture the requested response/UI/SQL/log evidence, and replace the partial/not-evidenced results with the observed per-case results. Do not treat the completion-record narrative as a substitute for that future live pass. **This live pass is now specified as [TEST_PLAN-2.md](./TEST_PLAN-2.md) (24/09/2026) - executable with `playwright-cli` and `chrome-devtools-axi`; case numbers mirror this plan 1:1 for direct status transcription.**

## Test Data

Seed this dataset once, before executing cases. Amounts are in MXN (the app formats via `Intl` es-MX / MXN). Months are relative: M = current month, M-1 = previous, and so on. Recompute expected values if seeding spans a month boundary - reseed if "today" crosses into a new month mid-run.

**User A** (`sprint4.user.a@example.com`) - primary dashboard user:

| Month | Type | Category | Amount | Day |
|-------|------|----------|--------|-----|
| M | Income | Salary | 15,000.00 | 1st |
| M | Expense | Rent | 6,000.00 | 2nd |
| M | Expense | Groceries | 1,850.50 | 10th |
| M | Expense | Transport | 320.75 | 15th |
| M | Expense | Entertainment | 480.00 | 20th |
| M-1 | Income | Salary | 15,000.00 | 1st |
| M-1 | Expense | Rent | 6,000.00 | 3rd |
| M-1 | Expense | Groceries | 2,100.00 | 12th |
| M-2 | Income | Salary | 15,000.00 | 1st |
| M-2 | Expense | Rent | 6,000.00 | 2nd |
| M-3 | (none - intentional gap month) | | | |
| M-4 | Income | Salary | 15,000.00 | 1st |
| M-4 | Expense | Groceries | 1,000.00 | 5th |

**User A budgets:**

| Budget | Category | Period | Limit | Current-month spend | Used |
|--------|----------|--------|-------|--------------------|------|
| Entertainment cap | Entertainment | Monthly | 500.00 | 480.00 | 96% (above 80% alert threshold) |
| Transport cap | Transport | Monthly | 2,000.00 | 320.75 | ~16% (below threshold) |

**User B** (`sprint4.user.b@example.com`) - isolation + negative balance:

| Month | Type | Category | Amount |
|-------|------|----------|--------|
| M | Income | Salary | 1,000.00 |
| M | Expense | Groceries | 5,000.00 |

**Hand-computed expected values (User A):**

- Monthly Income (M): 15,000.00 -> `$15,000.00`
- Monthly Expenses (M): 8,651.25 -> `$8,651.25`
- Total Balance: 60,000.00 income − 23,751.25 expenses = 36,248.75 -> `$36,248.75`
- 6-month trend rows (M -> M-5): (15,000 / 8,651.25), (15,000 / 8,100), (15,000 / 6,000), (0 / 0 gap-filled), (15,000 / 1,000), (0 / 0)
- Current-month category breakdown, descending: Rent 6,000.00; Groceries 1,850.50; Entertainment 480.00; Transport 320.75
- User B Total Balance: −4,000.00 -> `-$4,000.00`

---

## E2E Test Cases

### Case #1 - Dashboard summary cards show accurate aggregates

**Description:** Verify the three overview cards match hand-computed totals from the seeded data - the core trustworthiness check for the whole sprint.

**Status:** Recorded — partial (CR-2026-09-23)

**Steps:**

1. Log in as User A and navigate to `/` (Dashboard)
2. Read the three cards: Total Balance, Monthly Income, Monthly Expenses
3. Compare each against the hand-computed expected values in the Test Data section

**Expected outcome:** Cards show `$36,248.75`, `$15,000.00`, and `$8,651.25` respectively; amounts render in MXN currency format; no rounding drift beyond cents.

### Case #2 - User data isolation on dashboard and reports

**Description:** Confirm reporting endpoints are strictly user-scoped - one user must never see another's aggregates.

**Status:** Not evidenced (CR-2026-09-23)

**Steps:**

1. As User A, note the Dashboard card values
2. Log out, log in as User B, open the Dashboard
3. Compare the summary cards, pie chart, and line chart against User A's values

**Expected outcome:** User B's dashboard shows only User B's data (Monthly Expenses `$5,000.00`, one Groceries slice); none of User A's categories, amounts, or trend points appear.

### Case #3 - Income vs expenses trend renders contiguous months with zero-filled gaps

**Description:** Verify the 6-month line chart always shows exactly 6 contiguous points, with missing months zero-filled - the gap-filling contract of `DashboardService`.

**Status:** Recorded — partial (CR-2026-09-23)

**Steps:**

1. As User A, open the Dashboard and inspect the Income vs Expenses chart
2. Count the x-axis month labels and the points on each line
3. Check the M-3 (gap) and M-5 rows sit at zero on both lines
4. Hover a data point and read the tooltip

**Expected outcome:** Exactly 6 labels in `MMM yy` format (e.g., `Sep 26`); both lines have 6 points; M-3 and M-5 read `$0.00`; tooltip shows the month's income and expenses in MXN; income line renders green, expenses line red.

### Case #4 - Spending by category pie chart accuracy

**Description:** Verify the current-month breakdown excludes income, uncategorized, and soft-deleted transactions, and is ordered by total descending.

**Status:** Recorded — partial (CR-2026-09-23)

**Steps:**

1. As User A, open the Dashboard and inspect the Spending by Category pie
2. List the slices shown in the legend and their order
3. Hover slices and read the tooltip values

**Expected outcome:** Exactly 4 slices: Rent (6,000.00), Groceries (1,850.50), Entertainment (480.00), Transport (320.75) - descending by total; the Salary income category does not appear; tooltips show MXN amounts.

### Case #5 - Reports page month picker drives the category breakdown

**Description:** Verify switching months on `/reports` re-queries the breakdown (new query key) and the 12-month trend renders.

**Status:** Not evidenced (CR-2026-09-23)

**Steps:**

1. As User A, navigate to `/reports`
2. Confirm the month picker defaults to the current month and the pie matches Case #4's slices
3. Change the picker to M-1
4. Change the picker to M-3 (the empty month)
5. Inspect the Income vs Expenses chart on this page

**Expected outcome:** Step 3 shows the M-1 breakdown (Rent 6,000.00, Groceries 2,100.00) without a page reload; step 4 shows the empty-state message ("No spending data for this period."); the trend chart renders 12 contiguous monthly points with correct data where present and zero-filled gaps elsewhere.

### Case #6 - Transaction and category mutations invalidate dashboard queries

**Description:** Verify the cross-feature cache invalidation added in Sprint 4 - money edits must be reflected on the dashboard without a manual refresh.

**Status:** Not evidenced (CR-2026-09-23)

**Steps:**

1. As User A, open the Dashboard and note the Monthly Expenses card
2. Go to Transactions, create a new expense: Entertainment, 100.00, today
3. Navigate to the Dashboard and read Monthly Expenses and the pie legend
4. Go to Categories, rename "Entertainment" to "Leisure"
5. Return to the Dashboard and read the pie legend
6. Go to Categories, create a brand-new category, then return to the Dashboard

**Expected outcome:** After step 3 the card reads `$8,751.25` and the pie reflects the new total; after step 5 the legend shows "Leisure" (update invalidation); step 6 leaves the dashboard stable - creating a category neither breaks the page nor changes aggregates.

### Case #7 - Authentication guards protect reporting pages and endpoints

**Description:** Verify unauthenticated users cannot reach the dashboard, reports, or the reporting API.

**Status:** Recorded — partial (CR-2026-09-23)

**Steps:**

1. Log out; navigate directly to `/`
2. Navigate to `/reports`
3. With no bearer token, call all three endpoints (browser DevTools console, curl, or Scalar without auth):
   - `GET /api/reports/dashboard/summary`
   - `GET /api/reports/dashboard/income-vs-expenses?months=6`
   - `GET /api/reports/dashboard/category-breakdown?startDate=<today>&endDate=<today>`

**Expected outcome:** Steps 1-2 redirect to the login page; all three API calls return HTTP 401 Unauthorized with no data payload.

### Case #8 - Reporting API contract and parameter validation

**Description:** Verify the `ApiResponse<T>` envelope on success and the enveloped 400 with `INVALID_REPORT_PARAMETERS` on invalid parameters.

**Status:** Recorded — partial (CR-2026-09-23)

**Steps:**

1. As User A (or with User A's bearer token via Scalar), call `GET /api/reports/dashboard/summary` and inspect the response body shape
2. Call `GET /api/reports/dashboard/income-vs-expenses?months=6` and confirm 6 contiguous rows including a zero-filled row
3. Call with `months=0`, then `months=25`
4. Call `GET /api/reports/dashboard/category-breakdown` with no `startDate`/`endDate`
5. Call it with `startDate` after `endDate` (e.g., today + 30 days -> today)
6. Call with `months=abc` (non-numeric)

**Expected outcome:** Steps 1-2 return HTTP 200 with `{ isOk: true, data: ..., statusCode: 200, codeText: "OK" }` and camelCase DTO fields (`totalBalance`, `monthlyIncome`, `monthlyExpenses`, `year`, `month`, `income`, `expenses`, `categoryId`, `categoryName`, `total`). Steps 3-5 return HTTP 400 with the enveloped error (`isOk: false`, error title "Invalid Report Parameters", detail mentioning months 1-24 or the date range). Step 6 returns 400 and never a 500 - record the observed body shape; a bare binding ProblemDetails is tolerable here, but the service-level failures (steps 3-5) must be enveloped.

### Case #9 - New user empty state

**Description:** Verify a freshly registered user with zero transactions gets a clean, friendly dashboard rather than errors or NaN values - worst-case first-run experience.

**Status:** Not evidenced (CR-2026-09-23)

**Steps:**

1. Register a new user (`sprint4.user.c@example.com`) and log in
2. Open the Dashboard - inspect the three cards, the pie, and the line chart
3. Open `/reports`, keep the current month, inspect both charts
4. Switch to a past month

**Expected outcome:** All cards read `$0.00`; both charts show their empty-state messages ("No data available yet. Add some transactions to see the trend." and "No spending data for this period."); no error banners, no `NaN` or `$NaN` anywhere; step 4 behaves the same.

### Case #10 - Transactions date filter end-of-day inclusivity (Task 4 regression)

**Description:** Verify the latent-bug fix: date filtering works without a Npgsql `DateTimeKind` error, and a date-only end date includes same-day transactions.

**Status:** Passed (CR-2026-09-23)

**Steps:**

1. As User A, open the Transactions page
2. Filter with `startDate` = today and `endDate` = today
3. Filter with `startDate` = 1st of current month and `endDate` = last day of current month
4. Filter with `startDate` = 1st of M-1 and `endDate` = last day of M-1

**Expected outcome:** Step 2 returns today's transactions (the 100.00 Entertainment expense from Case #6 if done, otherwise the day's rows); step 3 returns all current-month transactions including any dated the final day of the month; step 4 returns the 3 M-1 transactions; no request 500s and no Npgsql `DateTimeKind` errors in backend logs at any point.

### Case #11 - Soft-deleted transactions are excluded from aggregates

**Description:** Verify deleting a transaction (soft delete) removes it from dashboard aggregates and the category breakdown.

**Status:** Not evidenced (CR-2026-09-23)

**Steps:**

1. As User A, note the Dashboard Monthly Expenses (should be `$8,751.25` if Case #6 ran, otherwise `$8,651.25`)
2. Go to Transactions and delete the 480.00 Entertainment expense
3. Return to the Dashboard and re-read the Monthly Expenses card and the pie legend

**Expected outcome:** Monthly Expenses drops by exactly 480.00; the Entertainment slice shows its reduced total (100.00 if Case #6 ran, otherwise the slice disappears); Total Balance increases by 480.00; charts update without a manual refresh (invalidation via delete mutation).

### Case #12 - Negative balance rendering

**Description:** Verify a user whose expenses exceed income sees a correctly formatted negative total - worst-case money state.

**Status:** Not evidenced (CR-2026-09-23)

**Steps:**

1. Log in as User B and open the Dashboard
2. Read the Total Balance card
3. Check the line chart's zero-income rendering for the current month

**Expected outcome:** The Total Balance card renders a negative amount (`-$4,000.00`) with intact formatting (minus sign, no `NaN`, no layout breakage); Monthly Expenses reads `$5,000.00`; the current-month trend point shows income at 0 and expenses at 5,000.00.

### Case #13 - MonthlyReportJob persists summaries idempotently

**Description:** Verify the scheduled job creates monthly summaries for the previous month on its first run and updates (never duplicates) on subsequent runs - the upsert contract.

**Status:** Passed (CR-2026-09-23)

**Steps:**

1. Temporarily change the `MonthlyReportJob` cron expression to `*/2 * * * *` (every 2 minutes) - never commit this change
2. Restart the API and wait for one job run; check backend logs for "MonthlyReportJob completed"
3. Query `reports.monthly_summaries` (`SELECT user_id, year, month, total_income, total_expenses, net_amount FROM reports.monthly_summaries ORDER BY year DESC, month DESC;`)
4. Wait for a second run and re-query
5. Revert the cron expression to `0 0 1 * *`, restart the API, confirm the seeded value in `ticker.cron_tickers`

**Expected outcome:** After the first run, a row exists for month M-1 for User A (income 15,000.00, expenses 8,100.00, net 6,900.00) and User B if B has prior-month data; after the second run the same rows are updated, not duplicated - exactly one row per `(user_id, year, month)` (the unique index holds); logs report the summary count; the committed cron is back to the 1st-of-month schedule.

### Case #14 - BudgetAlertJob logs budgets at or above 80% usage

**Description:** Verify the threshold scan only flags budgets at/above the 80% threshold and logs them with the data the alert payload needs.

**Status:** Not evidenced (CR-2026-09-23)

**Steps:**

1. Temporarily change the `BudgetAlertJob` cron expression to `*/2 * * * *` (every 2 minutes) - never commit this change
2. Ensure User A has the Entertainment budget at ~96% and the Transport budget at ~16% (see Test Data)
3. Restart the API and wait for a job run
4. Inspect backend logs for the "Budget alert" warning lines
5. Revert the cron expression to `0 */6 * * *` and restart

**Expected outcome:** A warning is logged for the Entertainment budget (user, budget name, category, ~96% usage, period, spent vs limit) and none for the Transport budget; the completion log states the count of flagged budgets; the job run completes without unhandled exceptions; the committed cron is back to the every-6-hours schedule.

### Case #15 - Database objects and seeded tickers are present and shaped correctly

**Description:** Verify the two Sprint 4 schema deliverables in PostgreSQL: the `reports` schema with `monthly_summaries` and the `ticker` operational store.

**Status:** Passed (CR-2026-09-23)

**Steps:**

1. Connect to the local PostgreSQL (psql or pgAdmin) and run:
   - `\dn` - confirm schemas `reports` and `ticker` exist
   - `\d reports.monthly_summaries` - inspect columns and indexes
   - `\dt ticker.*` - list TickerQ tables
   - `SELECT function_name, cron_expression, is_enabled FROM ticker.cron_tickers;` (adjust column names to the actual table shape if needed)
2. Verify `idx_monthly_summaries_user_period` is unique on `(user_id, year, month)`

**Expected outcome:** `reports.monthly_summaries` has `numeric(18,2)` amount columns, `timestamptz` audit columns, `idx_monthly_summaries_user_id`, and the unique period index; the `ticker` schema contains TickerQ's `time_tickers`, `cron_tickers`, and `cron_ticker_occurrences` tables; `cron_tickers` holds `generate-monthly-reports` and `check-budget-alerts`, both enabled, stored in TickerQ's seconds-expanded form (`0 0 0 1 * *` and `0 0 */6 * * *`).

---

## UI/UX Test Cases

### Case #16 - OverviewCards responsive layout

**Description:** Verify the three summary cards follow the mobile-first grid: stacked on phones, three columns from `md` up.

**Status:** Recorded — manual audit (CR-2026-09-23)

**Steps:**

1. On the Dashboard (User A), set the viewport to 320-600px wide - cards stack in one column
2. Resize to 768px+ (`md`) - cards render in three equal columns
3. Inspect one card at desktop width: title, amount, and icon layout

**Expected outcome:** One column below `md`, three equal columns at `md`+; each card shows the title (gray, small), the amount (large, bold, colored per card), and a circular colored icon chip on the right; no clipping or overflow at any width.

### Case #17 - Dashboard chart grid responsiveness

**Description:** Verify the pie and line charts stack on mobile and sit side-by-side from `lg` up, per the `grid-cols-1 lg:grid-cols-2` layout.

**Status:** Recorded — manual audit (CR-2026-09-23)

**Steps:**

1. On the Dashboard, resize across breakpoints: 320px, 600px, 900px, 1200px
2. At each width, check the two-chart row arrangement and each chart's internal rendering (labels, legend, axes)

**Expected outcome:** Below `lg` the charts stack vertically full-width; at `lg`+ they sit side-by-side in two columns; Recharts containers reflow cleanly - axis labels stay readable, the pie stays centered, legends never overlap chart content.

### Case #18 - 320px minimum viewport without horizontal scroll

**Description:** Verify the smallest supported viewport (320px) across both new pages - the UI design rules floor.

**Status:** Recorded — manual audit (CR-2026-09-23)

**Steps:**

1. In Chrome DevTools, set the viewport to exactly 320px wide
2. Visit the Dashboard and the Reports page
3. Scroll vertically through each page; check for a horizontal scrollbar at every scroll position
4. Verify the app shell (sidebar/header) on this viewport

**Expected outcome:** No horizontal scrolling on either page; content reflows to single-column; the navigation shell collapses to its icon bar on mobile; chart legends and month picker remain usable at this width.

### Case #19 - Loading skeleton states

**Description:** Verify all Sprint 4 components render skeleton placeholders while data loads - no blank flashes or layout jumps.

**Status:** Not evidenced (CR-2026-09-23)

**Steps:**

1. In DevTools, throttle the network (e.g., "Slow 3G") or briefly stop the backend
2. Load the Dashboard as User A and observe the loading phase
3. Inspect the skeletons in the DOM (accessibility attributes)

**Expected outcome:** Three gray pulsing card placeholders (with `aria-busy="true"` on the grid) and a pulsing rectangle for each chart (with `aria-label` like "Loading income vs expenses chart"); skeletons are replaced in place by real content with no jump; the page header renders immediately.

### Case #20 - Error state rendering

**Description:** Verify query failures surface friendly inline error banners on the dashboard and reports pages.

**Status:** Not evidenced (CR-2026-09-23)

**Steps:**

1. Stop the backend API process
2. Load the Dashboard (hard refresh) and observe every region
3. Load `/reports` and observe both charts
4. Restart the backend and retry (navigate away and back)

**Expected outcome:** The summary area shows the red inline banner ("Failed to load dashboard summary. Please try again."); each chart shows its own red banner ("Failed to load income vs expenses. Please try again." / "Failed to load spending by category. Please try again."); no white screen, no unhandled UI crash, no raw stack traces; after restart, retrying loads real data.

### Case #21 - Currency and month label formatting consistency

**Description:** Verify every monetary value and date label uses the shared formatters (MXN currency, `MMM yy` months) - no raw numbers or ISO dates leaking into the UI.

**Status:** Not evidenced (CR-2026-09-23)

**Steps:**

1. On the Dashboard, inspect the three card amounts
2. Inspect the line chart: y-axis ticks, tooltip values, x-axis labels
3. Inspect the pie chart: legend labels and tooltip values
4. On `/reports`, inspect the month picker's value format

**Expected outcome:** All amounts render as MXN currency (e.g., `$15,000.00`, negative as `-$4,000.00`) everywhere - cards, axes, tooltips, legends; x-axis labels use `MMM yy` (e.g., `Sep 26`); the month picker uses `yyyy-MM`; category names in the pie legend match the user's category names exactly.

### Case #22 - Chart accessibility attributes

**Description:** Verify charts expose `role="img"` with descriptive labels and decorative icons are hidden from assistive tech - the Sprint 4 accessibility contract.

**Status:** Passed (CR-2026-09-23)

**Steps:**

1. Inspect the Dashboard DOM: each chart wrapper has `role="img"` and a descriptive `aria-label`
2. Confirm the line chart's `aria-label` names the covered window (e.g., "last 6 months") and the reports page's instance says 12
3. Inspect the `OverviewCards` icons - each carries `aria-hidden="true"`
4. Tab through the page with a keyboard and (optionally) run a screen reader pass

**Expected outcome:** Both charts have `role="img"` with accurate `aria-label`s that reflect the rendered window; lucide icons are `aria-hidden`; the page remains fully keyboard-navigable; no interactive control relies on hover alone.

### Case #23 - Reports month picker usability

**Description:** Verify the month input is labeled, bounded, keyboard-operable, and guarded against empty submissions.

**Status:** Not evidenced (CR-2026-09-23)

**Steps:**

1. On `/reports`, confirm the input has a visible "Month" label
2. Change the month using only the keyboard (focus + arrow keys or typed value)
3. Attempt to clear the input completely and tab away
4. Attempt to type a month before the `min` bound (January 2000)

**Expected outcome:** The label is programmatically linked to the input; keyboard changes update the pie breakdown exactly like mouse use; clearing the input is ignored - the previous month persists and nothing is parsed or queried; the `min` bound prevents out-of-range selections; no console errors during any of this.

### Case #24 - Page titles and sidebar navigation

**Description:** Verify document titles update per page and the sidebar navigates correctly with active-state highlighting.

**Status:** Not evidenced (CR-2026-09-23)

**Steps:**

1. From the Dashboard, check the browser tab title
2. Click the Reports item in the sidebar; check the tab title and the sidebar's active state
3. Navigate back to Dashboard via the sidebar; check title and active state again
4. Deep-link directly to `/reports` in a fresh tab

**Expected outcome:** The tab title reflects the current page (via `setDocumentTitle`: "Dashboard" / "Reports"); the sidebar highlights the active destination; deep-linking to `/reports` works for an authenticated session; both pages are reachable from any other page of the app.

### Case #25 - Touch-target and mobile interaction audit

**Description:** Verify all interactive Sprint 4 controls meet the 44x44px minimum tap target and behave under touch.

**Status:** Recorded — partial manual audit (CR-2026-09-23)

**Steps:**

1. On a mobile viewport (375px+ or a real device), operate: sidebar navigation, the month picker, any visible buttons on the two pages
2. Measure tap areas in DevTools where uncertain
3. Interact with the charts by touch (tap a slice/point on a touch device or DevTools touch emulation)

**Expected outcome:** Every interactive control has a comfortable tap area of at least 44x44px; no mis-taps or overlapping hit areas; chart tooltips respond to taps; the icon-only mobile navigation is usable and labeled.

---

## Execution Notes

- Run the E2E section first, in order - Cases 6, 10, and 11 intentionally mutate the seeded data, and their expected values account for each other (noted in-line).
- If "today" crosses a month boundary mid-execution, re-verify Cases 1, 3, 4, and 10 against the shifted M.
- Record per-case evidence (screenshots, SQL query output, log excerpts) in the execution record; update each case status as it completes.
- After the full pass, re-run the pre-flight checks to confirm the tree is still clean, and confirm no temporary cron expression remains (`git diff backend/src/Modules` must show no job changes).

*Last Updated: 24/09/2026*
