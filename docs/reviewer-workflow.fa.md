<div dir="rtl">

# گردش کار بازبین فنی سیستم خزانه‌داری

> این سند **ترتیب کار** بازبین را می‌گوید: هر Pull Request از رسیدن تا تأیید از چه مراحلی می‌گذرد، اولویت هر مرحله چیست، در هر مرحله دقیقاً چه چیزی نگاه می‌شود و با کدام ابزار و کدام فایل‌های مخزن.
> توضیح ساده هر موضوع این سند، با منابع یادگیری، در `docs/reviewer-primer.fa.md` است. توضیح خود کد، الگوها و مسیر یادگیری در `docs/reviewer-guide.fa.md` است. ساخت و اجرای سیستم با Git و Docker در `docs/reviewer-run-guide.fa.md` است. این سند به آن دو ارجاع می‌دهد و آن‌ها را تکرار نمی‌کند.
> وضعیت مخزن در این سند مربوط به **۲ آذر ۱۴۰۵ (23 Nov 2026)** است: چهار سرویس (`cashflow`، `reference`، `accounts`، `requests`)، ۴۲ اسکریپت پایگاه داده، سه micro-frontend (`basic-info`، `receipts`، `requests`) و اسناد خزانه‌داری (پیش‌نمایش و «تایید سند»). اگر کد با این سند نخواند، کد و `CLAUDE.md` مرجع‌اند. سند را در همان Pull Request اصلاح کنید.

## ۰. اولویت‌ها چه معنایی دارند

| اولویت | معنا | اگر انجام نشود |
|---|---|---|
| **P1 — مانع** | در **هر** Pull Request، بدون استثنا | Pull Request تأیید نمی‌شود |
| **P2 — لازم** | در هر Pull Request که به آن بخش دست زده است (مثلاً پایگاه داده، فرانت‌اند، اسناد حسابداری) | تأیید فقط با توضیح در متن بازبینی که چرا لازم نبود |
| **P3 — دوره‌ای** | هفتگی، پیش از هر فاز تازه یا پیش از استقرار روی سرور مشتری | ریسک جمع می‌شود ولی یک Pull Request را متوقف نمی‌کند |

اگر وقت کم است، **مراحل P1 را به ترتیب جدول ۱ انجام دهید و بقیه را رها کنید**. هرگز ترتیب را برعکس نکنید: دیدن صفحه در مرورگر (مرحله ۱۱) جای خواندن آزمون‌ها (مرحله ۴) را نمی‌گیرد.

## ۱. نقشه کلی مراحل

| # | مرحله | اولویت | چه زمانی | ابزار اصلی | زمان تقریبی (Pull Request متوسط) |
|---|---|---|---|---|---|
| ۰ | آماده‌سازی محیط | یک بار | هفته اول | Git، Docker، .NET SDK، Node/pnpm، Python | ۱ روز |
| ۱ | دریافت و دسته‌بندی Pull Request | P1 | همان روز | GitHub، `git diff --stat` | ۱۰ دقیقه |
| ۲ | وضعیت CI | P1 | پیش از خواندن کد | GitHub Actions | ۵ دقیقه |
| ۳ | توضیح Pull Request و مستندات تصمیم | P1 | | `docs/decisions.md`، `docs/domain/*` | ۱۵ دقیقه |
| ۴ | آزمون‌ها، پیش از کد | P1 | | xUnit، pytest، Vitest | ۳۰ دقیقه |
| ۵ | لایه Domain | P1 | | C#، `InstrumentLifecycle.cs` | ۳۰ دقیقه |
| ۶ | لایه Application (تراکنش، idempotency، outbox) | P1 | | `CommandRunner.cs` | ۲۰ دقیقه |
| ۷ | Infrastructure و پایگاه داده | P2 | اگر SQL یا `db/postgres` عوض شده | Npgsql، PostgreSQL 17، pytest | ۳۰ دقیقه |
| ۸ | لایه Api | P2 | اگر endpoint عوض شده | ASP.NET Core Minimal APIs | ۱۰ دقیقه |
| ۹ | اسناد حسابداری | **P1** اگر سندی عوض شده | | `Vouchers.cs`، `posting-rules.md` | ۳۰ دقیقه |
| ۱۰ | فرانت‌اند | P2 | اگر `frontend/` عوض شده | Vue 3، Nuxt 4، Module Federation، Vitest | ۲۰ دقیقه |
| ۱۱ | اجرای دستی و دیدن در مرورگر، تلفن و تبلت | P2 | اگر صفحه یا رفتار کاربر عوض شده | Docker Compose، مرورگر (DevTools) | ۳۰ دقیقه |
| ۱۲ | ابزار مهاجرت | P2 | اگر `tools/migration` یا چرخه عمر سند عوض شده | Python، pytest | ۱۵ دقیقه |
| ۱۳ | استقرار و راهنماهای اجرا | P2 | اگر `deploy/`، سرویس، پورت یا متغیر `.env` عوض شده | Docker Compose، Nginx، PowerShell | ۱۵ دقیقه |
| ۱۴ | امنیت و داده | P1 | | `git diff`، جست‌وجو | ۱۰ دقیقه |
| ۱۵ | نوشتن نتیجه بازبینی | P1 | | GitHub Review | ۱۵ دقیقه |
| ۱۶ | پس از merge | P3 | | سرور آزمایشی، `docs/plan.md` | ۱۰ دقیقه |
| ۱۷ | بازبینی دوره‌ای | P3 | هفتگی و پیش از هر فاز | همه موارد بالا | ۲ تا ۴ ساعت در هفته |

زمان‌ها تخمین‌اند، برای برنامه‌ریزی هفته. Pull Requestهای بزرگ (مثل Pull Request ادغامی ۲۱ نوامبر) چند برابر وقت می‌خواهند. در آن حالت مرحله‌به‌مرحله پیش بروید و نتیجه هر مرحله را جدا در بازبینی بنویسید.

---

## مرحله ۰ — آماده‌سازی محیط (یک بار)

**هدف:** بتوانید هر شاخه را روی دستگاه خودتان build، آزمون و اجرا کنید.

| ابزار | نسخه‌ای که مخزن می‌خواهد | کجا تعیین شده | برای کدام مرحله |
|---|---|---|---|
| Git و حساب GitHub با دسترسی بازبین | — | تنظیمات مخزن | همه |
| .NET SDK | `10.0.100` به بالا (roll forward در همان نسخه اصلی) | `global.json` | ۴ تا ۸ |
| Docker و Docker Compose | Docker Engine تازه با Compose v2 | `deploy/docker-compose.yml` | ۷، ۱۱، ۱۳ |
| PostgreSQL 17 | از Docker (`postgres:17`)، نصب جدا لازم نیست | `deploy/docker-compose.yml`، `ci.yml` | ۷ |
| Node.js و pnpm | Node ‏22.21 به بالا، pnpm ‏10.20.0 | `frontend/package.json` (`engines`، `packageManager`) | ۱۰ |
| Python و pytest | Python 3.12، pytest 8 به بالا، psycopg 3 | `tools/migration/requirements.txt` | ۷، ۱۲ |
| ویرایشگر | JetBrains Rider یا VS Code با C# Dev Kit و Vue (Volar) | انتخاب شما | همه |
| ابزار پایگاه داده | `psql` (داخل کانتینر هست) یا DBeaver / pgAdmin | انتخاب شما | ۷، ۱۱ |
| مرورگر با DevTools | Chrome یا Firefox، حالت Device Toolbar | — | ۱۱ |
| سیستم‌عامل | لینوکس (یا WSL2 روی ویندوز). CI روی `ubuntu-latest` است | `ci.yml` | همه |

**کارها:**

1. `CLAUDE.md`، `docs/plan.md`، `docs/architecture.md` و شش ADR در `docs/adr/` را بخوانید.
2. `docs/reviewer-run-guide.fa.md` را از اول تا آخر روی یک clone تازه انجام دهید تا سیستم در `http://localhost:8080` بالا بیاید و `deploy/smoke-test.sh` بگوید `OK`.
3. فرمان‌های جدول «Commands» در `CLAUDE.md` را یک بار اجرا کنید. آزمون‌های آداپتور PostgreSQL را **با** `TREASURY_TEST_PG` اجرا کنید (فرمان در مرحله ۷).
4. فصل ۱۳ و ۱۴ `docs/reviewer-guide.fa.md` را نگه دارید؛ چک‌لیست ۱۳.۲ آن در مراحل ۵ تا ۱۴ همین سند پخش شده است.

---

## مرحله ۱ — دریافت و دسته‌بندی Pull Request — P1

**هدف:** پیش از خواندن کد بدانید این Pull Request به کدام بخش‌ها دست زده و کدام مراحل P2 برایش لازم است.

**ابزار:** صفحه Pull Request در GitHub (زبانه «Files changed»)، یا روی دستگاه خودتان:

<div dir="ltr">

```bash
git fetch origin <branch-name>
git diff --stat origin/main...FETCH_HEAD
```

</div>

**دسته‌بندی بر اساس مسیر فایل‌های تغییرکرده:**

| اگر این مسیر عوض شده | مراحل لازم |
|---|---|
| `src/Services/*/…Domain/` | ۵، و اگر `Vouchers/` هم عوض شده ۹ |
| `src/Services/*/…Application/`، `src/BuildingBlocks/` | ۶ |
| `src/Services/*/…Infrastructure*/`، `db/postgres/` | ۷ |
| `src/Services/*/…Api/` | ۸ |
| `src/Services/Cashflow/…/Vouchers/`، `docs/domain/posting-rules*.md`، شابلون اسناد | ۹ |
| `frontend/` | ۱۰ و ۱۱ |
| `tools/migration/`، `InstrumentLifecycle.cs` | ۱۲ |
| `deploy/`، `Dockerfile`ها، `.github/workflows/` | ۱۳ |
| فقط `docs/` | ۳ و ۱۴ (اگر `docs/domain/` است، نسخه `.fa.md` را هم ببینید) |

**نشانه‌ها که باید همین‌جا بپرسید:**

- Pull Request بیش از یک کار را با هم آورده است (قاعده ۸ `CLAUDE.md`). استثنا: وقتی مالک محصول صریحاً ادغام چند Pull Request را خواسته است، مثل Pull Request ادغامی ۲۱ نوامبر ۲۰۲۶.
- Pull Request به شاخه‌ای غیر از `main` می‌رود، یا مستقیم روی `main` push شده است.

---

## مرحله ۲ — وضعیت CI — P1

**هدف:** چیزی را که ماشین می‌تواند بسنجد، شما دوباره نسنجید؛ و اگر CI قرمز است، وقت خواندن کد را نگذارید.

**ابزار:** زبانه «Checks» در GitHub؛ فایل `.github/workflows/ci.yml`.

| job | چه چیزی را می‌سنجد | اگر قرمز بود کجا را ببینید |
|---|---|---|
| `dotnet` | build با `TreatWarningsAsErrors` (هر هشدار خطاست، `Directory.Build.props`)، همه آزمون‌های .NET **با PostgreSQL واقعی**، build حالت Offline | خروجی `dotnet test`؛ نام آزمون شکسته |
| `database-and-migration` | آزمون‌های طرح پایگاه داده (`db/postgres/tests`) و ابزار مهاجرت، از جمله یکسان بودن چرخه عمر سند در C# و Python | خروجی pytest |
| `frontend` | نصب با lockfile ثابت، Vitest (از جمله ترجمه همه کدهای خطا)، build همه بخش‌ها | خروجی `pnpm test` / `pnpm build` |
| `compose` | کل سیستم در Docker، `deploy/smoke-test.sh`، توقف و شروع دوباره با داده و smoke test دوباره | لاگ‌های آخر job (۱۰۰ خط آخر هر کانتینر) |

**قواعد:**

- CI قرمز ← بازبینی را با «Request changes» و نام job شکسته برگردانید. کد را نخوانید.
- CI سبز **شرط لازم است، نه کافی**. CI نمی‌فهمد قاعده‌ای که آزموده شده همان قاعده‌ای است که مالک محصول خواسته؛ مراحل ۳ تا ۵ برای همین است.
- اگر job در CI نیامده (مثلاً کسی `ci.yml` را تغییر داده و یک job حذف شده)، این خودش یافته P1 است.
- **کار باز (مالک محصول):** چهار job بالا باید در تنظیمات GitHub به‌عنوان required status check برای `main` علامت بخورند (`docs/plan.md`، کار ۴ فاز A). اگر هنوز نخورده‌اند، به مالک یادآوری کنید.

---

## مرحله ۳ — توضیح Pull Request و مستندات تصمیم — P1

**هدف:** بدانید این تغییر قرار است **کدام قاعده کسب‌وکار** را پیاده کند و **چه کسی** آن را تأیید کرده است.

**ابزار و فایل‌ها:**

| فایل | چه چیزی را در آن چک کنید |
|---|---|
| توضیح Pull Request (الگو: `.github/pull_request_template.md`) | هر دو بخش «What changed, in plain words» و «Technical notes» پر شده‌اند؛ چک‌لیست انتهای الگو تیک خورده و تیک‌ها راست می‌گویند |
| `docs/decisions.md` | هر قاعده تازه یک ردیف با تاریخ، تصمیم، دلیل و نام تأییدکننده (معمولاً «CEO, <date>») دارد |
| `docs/domain/open-questions.md` و `.fa.md` | هر چیزی که کد «فرض کرده» و مالک هنوز جواب نداده، اینجا به‌صورت پرسش آمده است؛ پرسش‌های جواب‌گرفته بسته شده‌اند |
| `docs/domain/*.md` و نسخه `*.fa.md` | هر دو با هم عوض شده‌اند (قاعده ۶ `CLAUDE.md`). جدول‌ها و عددها در دو زبان یکی‌اند |
| `docs/plan.md` | اگر کاری تمام شده، ردیف «Done log» و ستون «Status» به‌روز شده‌اند |
| `README.md` و `README.en.md` | اگر چیزی که ساخته شده عوض شده، «آخرین تغییرات» و «وضعیت فعلی» در هر دو به‌روز شده‌اند |
| `docs/adr/` | اگر یکی از «Fixed decisions» در `CLAUDE.md` عوض شده، ADR تازه دارد |

**پرچم قرمز:** قاعده‌ای که در توضیح Pull Request یا کد آمده ولی در `decisions.md` یا `open-questions.md` ردی از آن نیست. یعنی هوش مصنوعی قاعده را حدس زده است.

---

## مرحله ۴ — آزمون‌ها، پیش از کد — P1

**هدف:** مطمئن شوید هر قاعده آزمونی دارد و آزمون **واقعاً همان قاعده** را می‌سنجد. این مهم‌ترین مرحله است، چون آزمون‌ها تور ایمنی پروژه‌اند، نه اعتماد به نویسنده کد.

**ابزار:**

| نوع آزمون | چارچوب | کجا | اجرا روی دستگاه شما |
|---|---|---|---|
| دامنه | xUnit v3 (`xunit.v3` 3.2.2) روی Microsoft.Testing.Platform | `tests/*.Domain.Tests` | `dotnet test` |
| کاربرد (با آداپتورهای در حافظه) | xUnit | `tests/*.Application.Tests` | `dotnet test` |
| آداپتور PostgreSQL | xUnit + Npgsql | `tests/*.Infrastructure.Postgres.Tests` | مرحله ۷ |
| خواننده Excel کدینگ حساب‌ها | xUnit + ExcelDataReader | `tests/Treasury.Reference.Infrastructure.Excel.Tests` | `dotnet test` |
| معماری | xUnit (بررسی ارجاع اسمبلی‌ها) | `tests/Treasury.ArchitectureTests/LayerTests.cs` | `dotnet test` |
| طرح پایگاه داده | pytest + psycopg | `db/postgres/tests` | مرحله ۷ |
| مهاجرت | pytest | `tools/migration/tests` | مرحله ۱۲ |
| فرانت‌اند | Vitest 4 | `frontend/packages/ui-kit/src/*.test.ts` | `cd frontend && pnpm test` |
| کل سیستم | bash + curl | `deploy/smoke-test.sh` | مرحله ۱۱ |

**چه چیزی را چک کنید:**

1. **نام آزمون‌ها را مثل فهرست قواعد بخوانید.** نام هر آزمون یک جمله درباره رفتار سیستم است. از خودتان بپرسید: «اگر می‌خواستم این قاعده را بشکنم، کدام حالت آزموده نشده؟» (مرز مبلغ، تاریخ قبل/بعد از سررسید، سال مالی بسته، وضعیت غیرمجاز، ارز خارجی، فیلد خالی).
2. هر قاعده **هم حالت مجاز و هم حالت رد** را می‌سنجد. در حالت رد، آزمون **کد خطا** (مثل `Instrument.InvalidTransition`) و **تغییرنکردن وضعیت** را بررسی می‌کند، نه فقط اینکه «خطا داد».
3. آزمونی که فقط `Assert.True(result.IsSuccess)` دارد و هیچ‌چیز دیگری را نگاه نمی‌کند، آزمون نیست.
4. آزمونی حذف، skip یا سست نشده است تا CI سبز شود. در `git diff` دنبال حذف خطوط `[Fact]`، `[Theory]`، `def test_` و `it(` بگردید.
5. هر قاعده برگرفته از برنامه قدیم منبعش (فرم یا ماژول `Lcash.mdb`، یا فایل MVC) را در نام آزمون یا کامنت گفته است (قاعده ۱ `CLAUDE.md`).
6. برای SQL تازه آزمون آداپتور PostgreSQL هست؛ آزمون در حافظه SQL را نمی‌سنجد.
7. اگر Pull Request از قابلیتی از xUnit استفاده کرده که در `tests/Treasury.TestShim` نیست، باید همان را به shim هم اضافه کرده باشد (job `dotnet` با build حالت Offline این را می‌گیرد).

---

## مرحله ۵ — لایه Domain — P1

**هدف:** قاعده کسب‌وکار **فقط** اینجاست و درست پیاده شده است.

**ابزار:** C# 14 / .NET 10، ویرایشگر با «Go to definition» و «Find usages».

**فایل‌های کلیدی:**

| فایل | چرا مهم است |
|---|---|
| `src/Services/Cashflow/Treasury.Cashflow.Domain/Instruments/InstrumentLifecycle.cs` | **تنها منبع** تغییر وضعیت‌های مجاز چک و سفته (قاعده ۶) |
| `src/Services/Cashflow/Treasury.Cashflow.Domain/Instruments/Instrument.cs` | تجمیع اصلی: چک و سفته دریافتی و صادره، گام‌ها، «ثبت صیادی»، اقدام حقوقی |
| `src/Services/Cashflow/Treasury.Cashflow.Domain/Vouchers/Vouchers.cs` | اسناد خزانه‌داری (مرحله ۹) |
| `src/BuildingBlocks/Treasury.BuildingBlocks.Domain/` | `FiscalDate`، `Money`، `PersianText`، `DomainException`، `AggregateRoot` |
| `src/Services/{Reference,Accounts,Requests}/…Domain/` | اطلاعات پایه، حساب‌های بانکی و صندوق و تنخواه، درخواست وجه |

**چه چیزی را چک کنید:**

- قاعده در Domain است، نه در SQL، endpoint یا کامپوننت Vue. پرچم قرمز: `if` درباره وضعیت سند بیرون از Domain.
- کلاس دامنه setter عمومی ندارد (آزمون `Domain_types_expose_no_public_setters` آن را می‌گیرد) و تجمیع فقط از راه متدهای خودش ساخته و عوض می‌شود (`Receive`، `Restore`، …).
- خطاها `DomainException` با **کد ثابت** (`Area.Reason`) هستند، نه متن برای کاربر (قاعده ۴).
- تاریخ‌ها `FiscalDate` هستند، نه `string`. تجزیه متن شمسی فقط در `FiscalDate.ParseJalali` است؛ تجزیه‌کننده تازه نوشته نشده (قاعده ۵).
- مقایسه نام‌ها و شماره‌ها روی `PersianText.Key` است (یکسان‌سازی «ی/ي» و «ک/ك» و نیم‌فاصله).
- اگر `InstrumentLifecycle.cs` عوض شده: کپی Python در `tools/migration/treasury_migration/legacy_mapping.py`، هر دو فایل `docs/domain/instrument-lifecycle*.md` و ثبت در `open-questions.md` همراهش هست. تغییر چرخه عمر **تصمیم کسب‌وکار** است؛ مطمئن شوید مالک آن را تأیید کرده.
- مبلغ‌ها `decimal` یا `Money` هستند، هرگز `double`/`float`.

---

## مرحله ۶ — لایه Application — P1

**هدف:** هر فرمان یک تراکنش است، یک تجمیع را یک بار ذخیره می‌کند، دوباره‌کاری امن است و رویدادها گم نمی‌شوند (ADR 0002).

**فایل کلیدی:** `src/BuildingBlocks/Treasury.BuildingBlocks.Application/CommandRunner.cs`، و پوشه `Application` هر سرویس.

**چه چیزی را چک کنید:**

- هر نوشتن از `CommandRunner` می‌گذرد و **کلید یکتایی (Idempotency-Key)** می‌گیرد (قاعده ۳). آزمون کاربرد نشان می‌دهد ارسال دوباره با همان کلید سند دوم نمی‌سازد.
- یک فرمان **یک** تجمیع را **یک بار** ذخیره می‌کند. پرچم قرمز: دو بار ذخیره، یا «اگر خطا شد، قبلی را پاک کن».
- رویدادها در **همان تراکنش** به outbox می‌روند.
- ذخیره با بررسی نسخه ردیف است (کنترل همزمانی خوش‌بینانه). بپرسید: «اگر دو کاربر همزمان همین کار را بکنند چه می‌شود؟» اگر یکتایی لازم است، قید پایگاه داده هم پشت بررسی کاربرد هست.
- دسترسی به پایگاه داده بیرون از `IUnitOfWork.ExecuteAsync` نیست.
- سرویس‌ها جدول همدیگر را نمی‌خوانند؛ اطلاعات سرویس دیگر از راه پورت (مثلاً `accounts-svc` ← HTTP ← `reference-svc`) می‌آید و در آزمون با نسخه ساختگی جایگزین می‌شود.

---

## مرحله ۷ — Infrastructure و پایگاه داده — P2

**وقتی لازم است:** هر تغییر در `…Infrastructure.Postgres/` یا `db/postgres/`.

**ابزار:** Npgsql 10 (بدون ORM، SQL دست‌نویس)، PostgreSQL 17، pytest، `psql` یا DBeaver. اجرای آزمون‌ها با PostgreSQL واقعی:

<div dir="ltr">

```bash
docker run -d --name pg17 -e POSTGRES_PASSWORD=postgres -p 5432:5432 postgres:17
TREASURY_TEST_PG="Host=localhost;Username=postgres;Password=postgres" dotnet test
TREASURY_TEST_DSN="host=localhost user=postgres password=postgres dbname=treasury_test" pytest db/postgres/tests
```

</div>

بدون `TREASURY_TEST_PG` آزمون‌های آداپتور روی دستگاه شما **skip** می‌شوند؛ پس «همه سبز» روی دستگاه شما یعنی آن‌ها اجرا نشده‌اند.

**چه چیزی را چک کنید:**

- **همه SQLها پارامتری‌اند.** هیچ SQL با چسباندن رشته ساخته نشده (برنامه قدیم این کار را می‌کرد).
- اسکریپت تازه شماره بعدی را دارد (الان آخرین: `042_…`) و **قابل اجرای دوباره** است: `db-init` در هر شروع سیستم `apply.sh` را دوباره اجرا می‌کند. درس ۲۱ نوامبر: اسکریپت ۰۳۱ با هر شروع فهرست کوتاه‌تر وضعیت‌ها را برمی‌گرداند و سیستم با داده موجود بالا نمی‌آمد. قید تازه‌ای که داده قدیمی ممکن است نقضش کند، `NOT VALID` اضافه می‌شود و آزمون schema اسکریپت‌ها را روی چنین داده‌ای دوباره اجرا می‌کند.
- جدول تازه `GRANT`های نقش همان سرویس را دارد (`000_roles.sql`) و نقش سرویس دیگر به آن دسترسی ندارد. **هیچ foreign key یا query بین دو schema نیست.**
- جدول تراکنشی تازه بر اساس سال مالی **LIST-partition** شده و کلید پارتیشن در کلید اصلی است (ADR 0003، `003_fiscal_year_partitions.sql`).
- ستون تاریخ `date` است، نه `text`. ستون `company_id` اضافه نشده (ADR 0005: هر شرکت پایگاه داده خودش را دارد).
- فیلد تازه در کل زنجیره آمده: دامنه، snapshot، SQL خواندن و نوشتن، اسکریپت schema، و در صورت نیاز ابزار مهاجرت.
- آزمون آداپتور با **نقش همان سرویس** اجرا می‌شود، نه با `postgres`.

---

## مرحله ۸ — لایه Api — P2

**وقتی لازم است:** هر تغییر در `src/Services/*/…Api/` (`Program.cs`، `Endpoints.cs`).

**ابزار:** ASP.NET Core Minimal APIs، `Microsoft.AspNetCore.Authentication.JwtBearer`؛ برای امتحان سریع: `curl` یا فایل `.http` در ویرایشگر، و اجرای سرویس در حافظه:

<div dir="ltr">

```bash
dotnet run --project src/Services/Cashflow/Treasury.Cashflow.Api   # http://localhost:5081, /dev/... endpoints, GET /dev/outbox
```

</div>

**چه چیزی را چک کنید:**

- endpoint نازک است: ورودی را می‌گیرد، فرمان کاربرد را صدا می‌زند، نتیجه را برمی‌گرداند. هیچ قاعده کسب‌وکاری ندارد.
- نگاشت کد خطا به وضعیت HTTP در `ToHttp` (فایل‌های `*Endpoints.cs` هر سرویس) برای کد تازه درست است: نبودن کلید یکتایی ← 400، «پیدا نشد» (`*.NotFound`) ← 404، تعارض نسخه (`Concurrency.Conflict`) ← 409، در دسترس نبودن سرویس دیگر (`*.Unavailable`) ← 503، و بقیه (نقض قاعده) ← 422. کد «پیدا نشد» تازه‌ای که به فهرست اضافه نشده، به اشتباه 422 می‌دهد. بدنه خطا Problem Details است (RFC 9457).
- endpoint تازه زیر گروهی است که `RequireAuthorization` می‌گیرد. endpointهای `/dev` فقط در Development ثبت می‌شوند.
- تاریخ شمسی فقط در ورودی/خروجی API به متن تبدیل می‌شود.
- هر endpoint نوشتن هدر `Idempotency-Key` می‌خواهد.

---

## مرحله ۹ — اسناد حسابداری — P1 اگر سندی عوض شده

**چرا جدا آمده:** این بخش پول و دفتر کل را جابه‌جا می‌کند و با فاز F (`posting-svc`، ارسال سند به سیستم حسابداری) بزرگ‌تر می‌شود. خطای آن در آزمون‌های فنی دیده نمی‌شود، فقط در **بدهکار/بستانکار اشتباه**. بازبین این بخش باید اصول حسابداری دوطرفه، معین و تفصیل را بداند.

**فایل‌ها:**

| فایل | محتوا |
|---|---|
| `docs/domain/posting-rules.md` و `.fa.md` | جدول «شابلون اسناد خزانه داری» مالک: برای هر دریافت، پرداخت و عملیات، حساب بدهکار و بستانکار و نوع تفصیل هر طرف |
| `src/Services/Cashflow/Treasury.Cashflow.Domain/Vouchers/Vouchers.cs` | `Voucher`، `VoucherLine`، `VoucherTemplate`، `ReceiptVouchers` (سند هر دریافت و هر گام)، `ConfirmedVoucher` |
| `src/Services/Cashflow/Treasury.Cashflow.Application/Vouchers/VoucherUseCases.cs` | پیش‌نمایش و «تایید سند» |
| `src/Services/Cashflow/Treasury.Cashflow.Infrastructure.Postgres/PostgresVouchers.cs` | ذخیره سند تأییدشده (schema 030 و بعدی‌ها) |
| `tests/Treasury.Cashflow.Domain.Tests/VoucherTests.cs`، `tests/Treasury.Cashflow.Application.Tests/VoucherUseCaseTests.cs` | آزمون‌های سند |

**چه چیزی را چک کنید:**

1. **هر سند تازه یا تغییرکرده را با ردیف همان عملیات در جدول `posting-rules.md` تطبیق دهید:** حساب بدهکار، حساب بستانکار، تفصیل هر طرف (ذینفع، بانک مربوطه، صندوقدار، تنخواه‌دار، مرکز). جهت برگشت‌ها را جدا چک کنید؛ در ۱۸ نوامبر جهت سند «تحویل به صندوق از اقدام حقوقی» یک بار برعکس شد و با پرسش Q43 درست شد.
2. آزمون دامنه هر سند **حساب‌ها و مبلغ هر سطر** را می‌سنجد، نه فقط اینکه سندی ساخته شد.
3. سند تراز است: `TotalDebit == TotalCredit`. سند ناقص (حساب خالی) تأیید نمی‌شود (`Voucher.Incomplete`).
4. حساب‌های ثابت شابلون فقط حساب **فعال و قابل ثبت** کدینگ را می‌پذیرند (`VoucherTemplate.AccountNotFound` / `AccountInactive` / `AccountNotPostable`). حساب ثابت تازه در صفحه «شابلون اسناد خزانه داری» و در اسکریپت schema آمده است.
5. سند تأییدشده **عوض نمی‌شود** و دریافتِ دارای سند تأییدشده دیگر ابطال نمی‌شود. اصلاح فقط با سند جدید است.
6. هیچ کد حساب ثابتی (مثل `1101…`) در کد نیامده؛ کدها از شابلون و اطلاعات پایه می‌آیند (قاعده ۷).
7. سند ارز خارجی معادل ریالی دارد و مبلغ سطرها `decimal` است.
8. **اگر کد سیستم حسابداری یا `posting-svc` به مخزن اضافه شد:** همین گردش کار برای آن هم اجرا می‌شود، به‌علاوه: سرویس حسابداری schema و نقش خودش را دارد، ارسال سند به دفتر کل از راه outbox و با کلید یکتایی است (ارسال دوباره سند دوم نمی‌سازد)، و پذیرش «سند تراز با حساب‌های معتبر» در Domain همان سرویس است، نه در SQL. هدف فاز F: سندها در یک ماه موازی **تا ریال** با سیستم قدیم یکی باشند.

---

## مرحله ۱۰ — فرانت‌اند — P2

**وقتی لازم است:** هر تغییر در `frontend/`.

**ابزار:** Vue 3.5، Nuxt 4 (پوسته، فقط سمت کاربر)، Vite 8، Module Federation 2 (`@module-federation/vite`، `@module-federation/runtime`)، TypeScript 5.9، pnpm workspace، Vitest.

<div dir="ltr">

```bash
cd frontend && pnpm install --frozen-lockfile && pnpm test && pnpm build
```

</div>

**ساختار:** `frontend/shell` (پوسته Nuxt)، `frontend/remotes/{basic-info,receipts,requests}` (هر کدام یک micro-frontend)، `frontend/packages/ui-kit` (قالب‌بندی، ترجمه‌ها در `src/i18n.ts`، آزمون پوشش کدهای خطا در `src/codes.test.ts`).

**چه چیزی را چک کنید:**

- UI درباره قواعد تصمیم نمی‌گیرد. می‌تواند راهنمایی کند (مثلاً دکمه غیرفعال)، ولی تصمیم نهایی همیشه با سرور است و آزمون دامنه دارد.
- خطا را با **کد** نشان می‌دهد و ترجمه را از `i18n.ts` می‌گیرد؛ متن خام سرور نمایش داده نمی‌شود. هر کد تازه ترجمه فارسی و انگلیسی دارد (`codes.test.ts` در غیر این صورت می‌شکند).
- صفحه راست‌به‌چپ است، عددها و مبلغ‌ها با جداکننده و تاریخ‌ها شمسی‌اند (`format.ts`).
- **تلفن و تبلت (ADR 0006):** هر صفحه تازه یا تغییرکرده در عرض **۳۶۰ و ۷۶۸ پیکسل** قابل استفاده است (مرحله ۱۱).
- بسته تازه در `package.json` متن‌باز است و `pnpm-lock.yaml` همراهش به‌روز شده است.
- micro-frontend تازه در پوسته، دروازه (`deploy/gateway.conf`)، Docker Compose و CI ثبت شده است (مرحله ۱۳).

---

## مرحله ۱۱ — اجرای دستی و دیدن در مرورگر — P2

**وقتی لازم است:** هر تغییری که کاربر می‌بیند یا رفتار سیستم را عوض می‌کند.

**ابزار:** Docker Compose، `deploy/smoke-test.sh`، مرورگر با DevTools (حالت Device Toolbar).

**کارها** (جزئیات گام‌به‌گام: `docs/reviewer-run-guide.fa.md` بخش ۳ تا ۹):

1. شاخه Pull Request را بگیرید و کل سیستم را بالا بیاورید:

<div dir="ltr">

```bash
docker compose -f deploy/docker-compose.yml --env-file deploy/.env up -d --build
```

</div>

2. smoke test را اجرا کنید (فقط روی سرور آزمایشی؛ یک چک آزمایشی می‌نویسد):

<div dir="ltr">

```bash
deploy/smoke-test.sh
```

</div>

3. همان کاری را که بخش «What changed, in plain words» گفته، در `http://localhost:8080` خودتان انجام دهید. **حالت رد را هم امتحان کنید** (مثلاً واگذاری چک به بانک پیش از «ثبت صیادی» باید با پیام فارسی رد شود).
4. در DevTools عرض را روی ۳۶۰ و ۷۶۸ پیکسل بگذارید و ببینید فرم بدون اسکرول افقی پر و ذخیره می‌شود.
5. نسخه در حال اجرا (commit و تاریخ) بالای هر صفحه دیده می‌شود؛ مطمئن شوید همان commit Pull Request را می‌بینید.
6. سیستم را متوقف و دوباره شروع کنید و ببینید داده می‌ماند و سیستم بالا می‌آید (`stop` و سپس `up -d`).

---

## مرحله ۱۲ — ابزار مهاجرت — P2

**وقتی لازم است:** تغییر در `tools/migration/` یا در `InstrumentLifecycle.cs`، یا وضعیت تازه چک و سفته.

**ابزار:** Python 3.12، pytest، psycopg 3 (پایگاه جدید)، pymssql (خواندن پایگاه‌های قدیمی SQL Server مثل `Cashnajoo..`، فقط روی ماشین مهاجرت).

**چه چیزی را چک کنید:**

- نگاشت در `legacy_mapping.py` تابع خالص است و هر قاعده‌اش در `tests/test_mapping.py` آزمون دارد.
- آزمون یکسانی چرخه عمر (`test_mapping.py`، با خواندن `InstrumentLifecycle.cs`) سبز است و دور زده نشده.
- وضعیت قدیمی تازه (مثلاً `GoInlaw` ← «اقدام حقوقی») به وضعیت درست نگاشت شده است.
- **هیچ ردیفی بی‌صدا دور ریخته نمی‌شود:** ردیف رد شده با دلیل در گزارش می‌آید، و گزارش تطبیق (تعداد و جمع ریالی، مورد انتظار در برابر بارشده) کامل است.
- شناسه‌ها قطعی‌اند (اجرای دوباره ردیف تکراری نمی‌سازد).
- **P3 (کار باز فاز A، ۵):** اجرای آزمایشی مهاجرت روی کپی ناشناس‌شده `Cashnajoo86` که مالک بیرون از GitHub می‌دهد، و بررسی گزارش تطبیق. داده واقعی هرگز وارد مخزن نمی‌شود.

---

## مرحله ۱۳ — استقرار و راهنماهای اجرا — P2

**وقتی لازم است:** تغییر در `deploy/`، هر `Dockerfile`، `.github/workflows/`، یا اضافه شدن سرویس، پورت، صفحه یا متغیر `.env`.

**ابزار:** Docker Compose، Nginx (`nginx:1.29-alpine`، فایل `deploy/gateway.conf`)، Keycloak 26.4، GitHub Actions، PowerShell (`deploy/windows/treasury.ps1` و فایل‌های `.cmd` برای سرور آزمایشی مالک روی ویندوز).

**چه چیزی را چک کنید:**

- دروازه به‌طور پیش‌فرض فقط روی `127.0.0.1` گوش می‌دهد (`GATEWAY_ADDRESS`)، چون هنوز ورود کاربر فعال نیست. **تغییری که دروازه را بدون Keycloak به شبکه باز کند، پرچم قرمز امنیتی است.** همین‌طور تغییر `ASPNETCORE_ENVIRONMENT`.
- متغیر تازه `.env` در `deploy/.env.example` (با مقدار ساختگی) و در گام «Throw-away passwords» در `ci.yml` آمده است. خود `deploy/.env` هرگز commit نمی‌شود.
- سرویس تازه: نقش و گذرواژه پایگاه داده خودش، مسیر در `gateway.conf`، ردیف در `smoke-test.sh`، و آمدن در job `compose`.
- **دو راهنمای دستی در همان Pull Request به‌روز شده‌اند** (`CLAUDE.md`، «Where things are»): `docs/test-server-windows.md` (مالک، با دوبار کلیک) و `docs/reviewer-run-guide.fa.md` (بازبین، فارسی، فقط Git و Docker). اسکریپت‌های `deploy/windows/` هم همراه تغییر به‌روز شده‌اند.
- آزمون «توقف و شروع دوباره» در job `compose` سبز است (داده می‌ماند و اسکریپت‌های schema دوباره بی‌خطا اجرا می‌شوند).

---

## مرحله ۱۴ — امنیت و داده — P1

**هدف:** هیچ راز، داده مشتری یا الگوی ناامن وارد مخزن نشود. این مرحله را حتی برای Pull Requestی که فقط مستندات است انجام دهید.

**ابزار:** `git diff`، جست‌وجوی متن در GitHub یا روی دستگاه:

<div dir="ltr">

```bash
git diff origin/main...FETCH_HEAD --stat | grep -Ei '\.env$|\.mdb|\.bak|\.backup|\.sql\.gz|\.xlsx'
git diff origin/main...FETCH_HEAD | grep -Ein 'password=|pwd=|secret|apikey|api_key|bearer [a-z0-9]|user id=sa|;sa;'
```

</div>

**چه چیزی را چک کنید:**

- هیچ گذرواژه، کلید، `sa`، فایل `.env`، `.mdb`، پشتیبان پایگاه داده، فایل Excel واقعی یا داده واقعی شرکت ایران ناژو در تغییر نیست (قاعده ۷). آزمون‌ها داده ساختگی دارند.
- هیچ شناسه ثابت پایگاه داده (عدد یا GUID) در کد نیست.
- هیچ چیزی از `legacy/` کپی، build یا اجرا نشده است. `legacy/` فقط برای خواندن است.
- بسته تازه NuGet یا npm متن‌باز است و جزو بسته‌های ممنوع `CLAUDE.md` نیست: MediatR ≥13، AutoMapper ≥15، MassTransit ≥9، FluentAssertions ≥8.
- فایل بارگذاری‌شده (اسکن چک، مدارک اقدام حقوقی) نوع و اندازه‌اش در سرور چک می‌شود (الان PDF/JPG/PNG و حداکثر ۵ مگابایت).
- endpoint تازه بدون مجوز باز نمانده است (مرحله ۸).

---

## مرحله ۱۵ — نوشتن نتیجه بازبینی — P1

**ابزار:** GitHub، زبانه «Files changed»: comment روی خط، «Suggested change»، و در پایان «Review changes».

| نتیجه | چه زمانی |
|---|---|
| **Approve** | همه مراحل P1 و P2 لازم بدون یافته مانع انجام شده‌اند |
| **Request changes** | CI قرمز است، یا یکی از مراحل P1 یافته دارد، یا قاعده‌ای منبع یا آزمون ندارد |
| **Comment** | پرسش دارید و هنوز نمی‌دانید یافته است یا نه |

**قالب پیشنهادی متن بازبینی:**

<div dir="ltr">

```
Stages done: 1-6, 9, 14 (P1), 7, 10, 11 (P2). Skipped: 12, 13 (not touched).
Blocking:
  - [stage 4] Rule «…» has no rejection test (file:line).
Non-blocking:
  - [stage 10] …
Questions for the owner (plain words):
  - …
```

</div>

**چه کسی جواب چه پرسشی را می‌دهد:**

| پرسش | از چه کسی | چطور |
|---|---|---|
| «این قاعده کسب‌وکار درست است؟» | مالک محصول (مدیرعامل) | کوتاه و بدون اصطلاح فنی، یکی‌یکی؛ جواب در `docs/decisions.md` ثبت می‌شود |
| «چرا کد این‌طور نوشته شده؟»، «این حالت آزمون ندارد» | نویسنده کد (Claude Code) | comment روی همان خط در Pull Request |
| «برنامه قدیم چه می‌کرد؟» | خودتان، در `legacy/` | `legacy/README.md` می‌گوید کدام فرم یا فایل را ببینید |

قرار نیست بازبین درباره قاعده کسب‌وکار تصمیم بگیرد. کار بازبین این است که مطمئن شود هر قاعده **صاحب، منبع و آزمون** دارد.

**ادغام (merge):** با مالک محصول است. `main` محافظت‌شده است: یک تأیید انسانی + CI سبز. هرگز مستقیم روی `main` push نکنید.

---

## مرحله ۱۶ — پس از merge — P3

- سرور آزمایشی مالک (ویندوز، `docs/test-server-windows.md`) را با نسخه تازه `main` به‌روز کنید، یا به مالک بگویید «Start Treasury» را دوباره اجرا کند.
- مطمئن شوید `docs/plan.md` و `README.md`/`README.en.md` با چیزی که merge شد می‌خوانند.
- اگر در بازبینی کاری «برای بعد» ماند، در Pull Request بعدی یا در `docs/plan.md` ردش بماند.

---

## مرحله ۱۷ — بازبینی دوره‌ای — P3

هفتگی (یا پیش از هر فاز تازه) این‌ها را جدا از Pull Requestها نگاه کنید:

| کار | ابزار | چرا |
|---|---|---|
| کل سیستم را از یک clone تازه از `main` بالا بیاورید و smoke test بگیرید | `docs/reviewer-run-guide.fa.md` | Pull Requestها جدا سبزند ولی با هم ممکن است نخوانند |
| `docs/domain/open-questions.md`: پرسش‌های بی‌جواب و کدهایی که بر فرض آن‌ها ساخته شده | خواندن | پیش از پایلوت هیچ فرض بازی نباید در مسیر پول بماند |
| به‌روزرسانی‌های امنیتی بسته‌ها (NuGet، npm، تصویرهای Docker) | `dotnet list package --vulnerable`، `pnpm audit` | سیستم مالی است |
| تنظیمات GitHub: محافظت `main`، required checks، دسترسی‌ها | GitHub Settings | کار ۴ فاز A |
| پیش از Keycloak و چندشرکتی (ADR 0005): تفکیک داده هر شرکت، نقش‌ها و حقوق کاربر | ADR 0005، `Program.cs` هر سرویس | بزرگ‌ترین تغییر امنیتی پیش رو |
| پیش از استقرار روی سرور مشتری: پشتیبان‌گیری، بازگردانی، گذرواژه‌ها، باز بودن دروازه | `deploy/` | بازبین نگه‌دارنده دسترسی سرورهاست (سند طراحی ۱۳.۲) |
| پیش از بهره‌برداری: آزمون نفوذ مستقل | شرکت متخصص بیرونی | سند طراحی ۱۳.۲ |

---

## تقسیم کار بین دو بازبین (پیشنهاد)

با آمدن بازبین دوم، هر Pull Request همچنان **یک تأیید نهایی** می‌خواهد، ولی مراحل را می‌شود تقسیم کرد. پیشنهاد:

| | بازبین اول (اصلی) | بازبین دوم |
|---|---|---|
| مراحل | ۱ تا ۸، ۱۲، ۱۴، ۱۵ | ۹ (اسناد حسابداری)، ۱۰ و ۱۱ (فرانت‌اند، مرورگر، تلفن)، ۱۳ (استقرار و راهنماها) |
| مهارت لازم | .NET 10 و C#، PostgreSQL، DDD و معماری تمیز، لینوکس | اصول حسابداری دوطرفه و کدینگ معین/تفصیل، Vue/TypeScript، Docker |
| تأیید نهایی | با او؛ پس از اینکه بازبین دوم بخش خودش را در comment تأیید کرد | comment با «Stages 9, 10, 11, 13: OK» یا یافته‌ها |

مرحله ۱۴ (امنیت) را **هر دو** انجام دهند؛ دو جفت چشم برای رازها و داده مشتری هزینه کمی دارد. تقسیم دقیق کار تصمیم مالک محصول است؛ این جدول فقط نقطه شروع است.

عمق دانش لازم در هر تخصص (۱ تا ۱۰) برای هر دو نقش، و آزمون‌های عملی پیشنهادی برای مصاحبه: `docs/reviewer-skills.fa.md`.

## جدول یک‌جای ابزارها

| ابزار / فناوری | نسخه در مخزن | کجا در مخزن | مراحل |
|---|---|---|---|
| .NET SDK / C# | 10.0.100+ | `global.json`، `Directory.Build.props` | ۴–۹ |
| ASP.NET Core Minimal APIs + JwtBearer | 10.0.0 | `src/Services/*/…Api` | ۸ |
| Npgsql (بدون ORM) | 10.0.3 | `…Infrastructure.Postgres` | ۷ |
| ExcelDataReader | 3.9.0 | `Treasury.Reference.Infrastructure.Excel` | ۴، ۷ |
| xUnit v3 + Microsoft.Testing.Platform | 3.2.2 | `tests/`، `global.json` | ۴ |
| TestShim (جایگزین xUnit در حالت Offline) | — | `tests/Treasury.TestShim`، `Treasury.Offline.slnf` | ۴ |
| PostgreSQL | 17 | `db/postgres`، `deploy/docker-compose.yml` | ۷ |
| Python + pytest + psycopg / pymssql | 3.12، pytest ≥8 | `db/postgres/tests`، `tools/migration` | ۷، ۱۲ |
| Node.js + pnpm | ≥22.21، pnpm 10.20.0 | `frontend/package.json` | ۱۰ |
| Nuxt / Vue / Vite / TypeScript | 4.6.4 / 3.5.45 / 8.3.7 / 5.9 | `frontend/shell`، `frontend/remotes` | ۱۰ |
| Module Federation | vite 1.23.11، runtime 2.9.3 | `frontend/remotes/*/vite.config.ts`، پوسته | ۱۰ |
| Vitest | 4 | `frontend/packages/ui-kit` | ۴، ۱۰ |
| Docker + Compose | Compose v2 | `deploy/docker-compose.yml`، `Dockerfile`ها | ۱۱، ۱۳ |
| Nginx (دروازه) | 1.29-alpine | `deploy/gateway.conf` | ۱۳ |
| Keycloak | 26.4 | `deploy/docker-compose.yml` | ۸، ۱۳، ۱۷ |
| GitHub Actions | — | `.github/workflows/ci.yml` | ۲ |
| PowerShell / cmd (سرور آزمایشی ویندوز) | — | `deploy/windows/` | ۱۳ |
| bash + curl (smoke test) | — | `deploy/smoke-test.sh` | ۱۱، ۱۳ |

</div>
