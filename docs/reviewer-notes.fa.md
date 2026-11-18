<div dir="rtl">

# یادداشت‌های نوشته‌شده برای بازبین

«Technical notes» از pull requestهای ۱ تا ۱۴، گردآوری در ۲۷ آبان ۱۴۰۵ (<span dir="ltr">18 Nov 2026</span>). نسخه انگلیسی: [reviewer-notes.md](reviewer-notes.md).

> این سند تصویر لحظه‌ای ۲۷ آبان ۱۴۰۵ (<span dir="ltr">18 Nov 2026</span>) است. pull requestهای بعدی در آن نیستند؛ یادداشت‌های آن‌ها در صفحه هر pull request روی GitHub است.

## ۱. این سند چیست

هر pull request در این پروژه بخشی به نام **«Technical notes»** دارد که برای بازبین نوشته شده است: انتخاب‌های طراحی، ریسک‌ها، آزمون‌ها، و آنچه انجام یا بررسی نشده. این سند آن یادداشت‌ها را از هر ۱۴ pull request، به ترتیب و در یک‌جا گرد آورده است.

- متن انگلیسی بخش ۴ بدون تغییر از GitHub برداشته شده است. متن فارسی ترجمه همان است. اگر این دو با هم فرق داشتند، متن انگلیسی روی GitHub مرجع است.
- در ۲۷ آبان ۱۴۰۵ (<span dir="ltr">18 Nov 2026</span>) روی GitHub بررسی شد: هیچ‌کدام از ۱۴ pull request نه review دارد، نه comment و نه یادداشت روی خط کد. پس «Technical notes» همه چیزی است که تا امروز روی GitHub برای بازبین نوشته شده.
- دو راهنمای بلندتر برای بازبین از پیش در مخزن هست (هر دو فارسی):
    - [docs/reviewer-guide.fa.md](https://github.com/BehinTaghavi/treasury/blob/main/docs/reviewer-guide.fa.md): کد چطور سازمان یافته و در هر pull request چه چیزهایی را باید نگاه کرد؛
    - [docs/reviewer-run-guide.fa.md](https://github.com/BehinTaghavi/treasury/blob/main/docs/reviewer-run-guide.fa.md): ساخت و اجرای هر نسخه با Git و Docker.

## ۲. چطور آخرین نسخه را بگیریم

مخزن عمومی (public) است، پس این پیوندها بدون دعوت‌نامه کار می‌کنند.

| چه چیزی | پیوند یا دستور |
|---|---|
| پروژه (آخرین نسخه تأییدشده، شاخه `main`) | <https://github.com/BehinTaghavi/treasury> |
| pull requestهای در انتظار بازبینی | <https://github.com/BehinTaghavi/treasury/pulls> |
| همان نسخه به شکل فایل ZIP | <https://github.com/BehinTaghavi/treasury/archive/refs/heads/main.zip> |

**بار اول، در ترمینال** (در ویندوز Git Bash):

```bash
git clone -c core.autocrlf=false https://github.com/BehinTaghavi/treasury.git
cd treasury
```

**هر بار پس از آن**، برای گرفتن تازه‌ترین `main`:

```bash
git switch main
git pull
```

**برای دیدن یک pull request که هنوز ادغام نشده** (برای نمونه شماره ۱۴):

```bash
git fetch origin pull/14/head:pr-14
git switch pr-14
```

**برای ساخت و اجرای آنچه روی دیسک است** (Docker لازم است؛ جزئیات در راهنمای اجرا):

```bash
cp deploy/.env.example deploy/.env
docker compose -f deploy/docker-compose.yml --env-file deploy/.env up -d --build
deploy/smoke-test.sh
```

سپس <http://localhost:8080> را باز کنید.

**تازه‌ترین کار کجاست.** شاخه `main` شامل pull requestهای ۱ تا ۹ است (آخرین ادغام: ۲۴ آبان ۱۴۰۵ / <span dir="ltr">15 Nov 2026</span>). کارهای پس از آن در پنج pull request باز، شماره ۱۰ تا ۱۴ است و هنوز در `main` نیست. برای دیدن آن‌ها، pull request را به روش بالا بگیرید.

**اگر بازبین با یک دستیار هوش مصنوعی برنامه‌نویسی کار می‌کند** (مثلاً Claude Code)، همین یک prompt آن را به‌روز می‌کند:

```text
Clone https://github.com/BehinTaghavi/treasury.git (or run "git pull" on main
if it is already cloned). Read CLAUDE.md, README.en.md, docs/plan.md and
docs/reviewer-guide.fa.md. Then list the open pull requests at
https://github.com/BehinTaghavi/treasury/pulls and, for each one, show me its
"Technical notes" and the files it changes. Do not push, merge or comment
on anything.
```

## ۳. اول این‌ها را بخوانید

این نکته‌ها از یادداشت‌های بخش ۴ و از وضعیت GitHub در ۲۷ آبان ۱۴۰۵ (<span dir="ltr">18 Nov 2026</span>) گرفته شده‌اند.

**۳.۱ هنوز هیچ بازبینی‌ای ثبت نشده است.** pull requestهای ۱ تا ۹ ادغام شده‌اند و GitHub برای هیچ‌کدام review نشان نمی‌دهد. بزرگ‌ترین آن‌ها شماره ۸ است (۴۵ commit، ‏۲۵۱ فایل، حدود ۲۴٬۶۰۰ خط افزوده)؛ یادداشت‌های شماره ۹ می‌گوید بازبینی آن هنوز باقی است.

**۳.۲ پنج pull request باز است و چهارتای آن‌ها با هم هم‌پوشانی دارند.** یادداشت‌های شماره ۱۲ و ۱۴ این هم‌پوشانی‌ها را شرح می‌دهند:

| شماره‌ها | هم‌پوشانی | یادداشت‌ها چه می‌گویند |
|---|---|---|
| ۱۱ و ۱۴ | هر دو قاعده «ثبت در صیاد پیش از بانک» را می‌سازند. شماره ۱۱ به شکل یک تیک روی چک (مالک، ۲۴ آبان)؛ شماره ۱۴ به شکل یک مرحله و وضعیت، قاعده N3 (مالک، ۲۷ آبان). | فقط یکی باید ادغام شود. شماره ۱۴ از قاعده بعدی مالک پیروی می‌کند. |
| ۱۲ و ۱۴ | هر دو نام و نشانی شرکت را در `reference-svc` نگه می‌دارند («اطلاعات شرکت» در ۱۲، «مشخصات شرکت» در ۱۴). | پیش از ادغام هر دو، باید یک صفحه شوند. |
| ۱۰ و ۱۴ | هر دو همان کد چرخه عمر، رسید و سند حسابداری را تغییر می‌دهند. ادغام آزمایشی ۲۶ فایل دارای تعارض داد که چندتای آن‌ها تعارض واقعی طراحی است. | یا شماره ۱۰ باید با ۱۴ سازگار شود، یا برعکس. |
| ۱۰، ۱۱، ۱۲، ۱۴ | اسکریپت‌های شِمای 033 به بعد و شماره پرسش‌های Q37 به بعد در بیش از یکی از آن‌ها به کار رفته است. | هر کدام که دیرتر ادغام شود پرسش‌هایش (و ترجیحاً اسکریپت‌هایش) را دوباره شماره‌گذاری می‌کند. |

شماره ۱۳ (نشان نسخه، اصلاح gateway) در هیچ‌کدام از این هم‌پوشانی‌ها نام برده نشده است. شماره ۱۴ به‌صورت محلی و ادغام‌شده با ۱۳ آزموده شده است.

*یک ترتیب ممکن، فقط به‌عنوان پیشنهاد (تصمیم با بازبین و مالک است):* اول ۱۳؛ سپس ۱۴؛ اگر ۱۴ پذیرفته شد ۱۱ بسته شود؛ شماره ۱۲ و صفحه شرکت در ۱۴ یک صفحه شوند؛ سپس ۱۰ با نتیجه سازگار شود.

**۳.۳ وضعیت CI در pull requestهای باز** (GitHub، ‏۲۷ آبان ۱۴۰۵): هر چهار job در شماره‌های ۱۰، ۱۱، ۱۲ و ۱۳ سبز است. در شماره ۱۴، ‏job با نام `compose` شکست خورده و سه job دیگر موفق‌اند ([اجرای ناموفق](https://github.com/BehinTaghavi/treasury/actions/runs/44247305117/job/134609249577)). یادداشت‌های شماره ۱۴ به این شکست اشاره نکرده‌اند.

**۳.۴ چیزهایی که یادداشت‌ها می‌گویند آزموده یا انجام نشده:**

- **ویندوز واقعی.** فایل‌های `.cmd`، راه‌اندازی خودکار Docker Desktop و Windows PowerShell 5.1 روی ماشین ویندوز واقعی اجرا نشده‌اند (۱). اسکریپت ویندوز برای نشان نسخه اجرا نشده است (۱۳).
- **ورود.** هنوز ورود (login) وجود ندارد؛ نشانی‌های همه سرویس‌ها باز است. فقط داده آزمایشی (۳، ۵، ۱۲).
- **pull request شماره ۸.** آزمون‌ها هنگام باز کردن آن دوباره اجرا نشدند؛ CI روی همان pull request نخستین اجرای آن‌ها بود. نتیجه CI به نقل از شماره ۹: ‏۵۱۰ آزمون ‎.NET، ‏۵۰۹ موفق، ۱ ردشده.
- **PostgreSQL 16 به‌جای 17 روی ماشین محلی،** و اجرا نشدن Docker Compose با دست (۱۰، ۱۱، ۱۲). CI هر دو را روی PostgreSQL 17 و در job با نام `compose` اجرا کرده است.
- **دو کمبود شناخته‌شده در آداپتور PostgreSQL** (۱): رد شدن توسط تریگر سال بسته به شکل HTTP 500 بیرون می‌آید، نه کد پایدار؛ مسیر `ChequeBook.SheetAlreadyUsed` از نقض یکتایی آزمون ندارد.
- **سفته** (۱۰): ظهرنویسی سفته و سند حسابداری پس‌دادن سفته عادی فقط با آزمون پوشش دارند و در مرورگر دیده نشده‌اند. تعویض واژه‌ها در `ChequeOperations.vue` با جایگزینی متن انجام می‌شود.
- **راهنمای اجرا** (۹): دستورهای آن پس از به‌روزرسانی دوباره با دست اجرا نشده‌اند.

## ۴. یادداشت‌های هر pull request

| شماره | عنوان | وضعیت |
|---|---|---|
| [۱](#pr-1) | فاز الف، کارهای ۱ تا ۴ و ۶: نخستین ساخت واقعی، آزمون‌های پایگاه داده، Docker Compose، CI و سرور آزمایشی روی رایانه مالک | ادغام‌شده، ۲۴ آبان ۱۴۰۵ |
| [۲](#pr-2) | بازبینی مالک از دریافت‌ها: ترتیب منوی اصلی و قاعده N2 (ثبت وصول یا برگشت پیش از سررسید ممنوع) | ادغام‌شده، ۲۴ آبان ۱۴۰۵ |
| [۳](#pr-3) | اطلاعات پایه، گام ۱: ارزها، کشور/استان/شهر، نام بانک‌ها (reference-svc) | ادغام‌شده، ۲۴ آبان ۱۴۰۵ |
| [۴](#pr-4) | اطلاعات پایه، گام ۲: کدینگ حساب‌ها و تفصیل از سیستم حسابداری (ورود از Excel) | ادغام‌شده، ۲۴ آبان ۱۴۰۵ |
| [۵](#pr-5) | اطلاعات پایه، گام ۳: حساب‌های بانکی شرکت (accounts-svc) | ادغام‌شده، ۲۴ آبان ۱۴۰۵ |
| [۶](#pr-6) | مشترک‌کردن زیرساخت اجرای فرمان در سه سرویس (بدون تغییر رفتار) | ادغام‌شده، ۲۴ آبان ۱۴۰۵ |
| [۷](#pr-7) | مستندات: راهنمای بازبین به فارسی | ادغام‌شده، ۲۴ آبان ۱۴۰۵ |
| [۸](#pr-8) | کار ۲۰ تا ۲۴ آبان (<span dir="ltr">11–15 Nov</span>): تکمیل اطلاعات پایه، موجودی اول دوره، دریافت‌ها، درخواست‌های پرداخت، اسناد حسابداری، مراحل چک | ادغام‌شده، ۲۴ آبان ۱۴۰۵ |
| [۹](#pr-9) | مستندات: صفحه اصلی فارسی و راهنمای ساخت و اجرای بازبین، هم‌گام با main امروز | ادغام‌شده، ۲۴ آبان ۱۴۰۵ |
| [۱۰](#pr-10) | سفته: فرم دریافت با رسید چاپی، مراحل پس از دریافت، و صاحبان امضای مجاز | باز از ۲۵ آبان ۱۴۰۵، CI سبز |
| [۱۱](#pr-11) | «ثبت صیادی شد»: چک فقط پس از تیک «ثبت در صیاد» به بانک می‌رود | باز از ۲۶ آبان ۱۴۰۵، CI سبز |
| [۱۲](#pr-12) | «اطلاعات شرکت»: نام و نشان شرکت بالای همه صفحه‌ها، همراه با مشخصات رسمی | باز از ۲۶ آبان ۱۴۰۵، CI سبز |
| [۱۳](#pr-13) | نمایش نسخه در حال اجرا بالای صفحه و در پنجره «Start Treasury» | باز از ۲۶ آبان ۱۴۰۵، CI سبز |
| [۱۴](#pr-14) | اقدام حقوقی برای چک‌های برگشتی، قاعده N3 («ثبت صیادی» پیش از بانک)، گواهی عدم پرداخت، فهرست دریافت‌ها، مشخصات شرکت | باز از ۲۷ آبان ۱۴۰۵، CI: **`compose` ناموفق** |

<a id="pr-1"></a>

### pull request شماره ۱

**فاز الف، کارهای ۱ تا ۴ و ۶: نخستین ساخت واقعی، آزمون‌های پایگاه داده، Docker Compose، CI و سرور آزمایشی روی رایانه مالک**

ادغام‌شده، ۲۴ آبان ۱۴۰۵ · شاخه `claude/adoring-knuth-2m722k` · https://github.com/BehinTaghavi/treasury/pull/1

**ساخت / ‎.NET**

- SDK: بسته `dotnet-sdk-10.0` نسخه 10.0.114 از مخزن Ubuntu (نشانی `builds.dotnet.microsoft.com` در محیط ابری بسته است؛ NuGet بسته نیست). این نسخه با `global.json` (10.0.100، `latestFeature`) سازگار است. CI مثل قبل از `setup-dotnet` استفاده می‌کند.
- نخستین ساخت با NuGet هیچ هشداری نداشت، پس `TreatWarningsAsErrors` برای `Infrastructure.Postgres` دوباره روشن شد. این پروژه اکنون به‌جای کل `Microsoft.AspNetCore.App` فقط از `Microsoft.Extensions.DependencyInjection.Abstractions` استفاده می‌کند. آزمون معماری تازه: هیچ `Treasury.Cashflow.Infrastructure*.dll` (با جست‌وجوی فایل پیدا می‌شود، پس آفلاین و آنلاین کار می‌کند) به ASP.NET Core یا Api ارجاع ندارد.
- **اشکال:** `InstrumentUseCases.GetAsync` مخزن (repository) را بیرون از `IUnitOfWork.ExecuteAsync` صدا می‌زد. آداپتورهای Npgsql به تراکنش جاری نیاز دارند و `InvalidOperationException` می‌دادند. نسخه درون‌حافظه‌ای این اشکال را پنهان می‌کرد. اکنون GetAsync درون یک unit of work اجرا می‌شود. بررسی کردم: با برگرداندن این اصلاح، ۴ آزمون آداپتور شکست می‌خورند.

**آزمون‌های آداپتور PostgreSQL** (`tests/Treasury.Cashflow.Infrastructure.Postgres.Tests`)

- یک assembly fixture از xUnit v3 پایگاه `treasury_adapter_test_<pid>` را می‌سازد، فایل‌های `db/postgres/0??_*.sql` را به همان شیوه `apply.sh` اجرا می‌کند (نقش‌ها با کاربر مدیر، اشیا با `treasury_owner`) و سال‌های ۱۴۰۳ تا ۱۴۰۵ را باز می‌کند. آداپتورها با `Options=-c role=cashflow_svc` اجرا می‌شوند، پس نبودن یک مجوز (grant) باعث شکست آزمون می‌شود. پایگاه در پایان حذف می‌شود.
- اتصال از متغیر `TREASURY_TEST_PG` (قالب Npgsql) می‌آید. بدون آن، آزمون‌ها روی رایانه محلی **رد می‌شوند (skip)** و وقتی `CI` تنظیم شده باشد **شکست می‌خورند**، تا CI نتواند بی‌صدا از آن‌ها بگذرد. روی CI: ۸۲ موفق، ۰ ردشده.
- پوشش: رفت‌وبرگشت فیلدها (شامل ارز خارجی، نرخ و مبلغ ریالی)، تاریخچه، outbox از نوع jsonb با تاریخ ISO، ردیف idempotency، هشدار تکراری، ثبت مراحل در پارتیشن سالی که در آن رخ می‌دهند (۱۴۰۴ ← ۱۴۰۵)، rollback هنگام نقض قاعده، هم‌زمانی خوش‌بینانه، تریگر سال بسته به‌عنوان آخرین سد، ترتیب برگه‌ها، بسته‌شدن دسته‌چک با آخرین برگه، ۲۵ صدور هم‌زمان زیر قفل واقعی ردیف، و آزاد ماندن برگه پس از صدور ردشده.
- **پوشش داده نشده:** مسیر تبدیل unique-violation جدول `cheque_sheet` به `ChequeBook.SheetAlreadyUsed`. تا وقتی قفل `FOR UPDATE` برقرار است نمی‌توان به آن رسید (درج FK پشت قفل می‌ماند). همچنین رد شدن توسط تریگر سال بسته به شکل `PostgresException` خام (HTTP 500) از use case بیرون می‌آید، نه یک کد پایدار. فعلاً پذیرفتنی است، چون بررسی دوره مالی در لایه Application زودتر اجرا می‌شود. هنگام ساخت posting-svc ارزش نگاشت به کد پایدار را دارد.

**شِما (اشکال راه‌اندازی دوباره)**

- `apply.sh` در اجرای دوم شکست می‌خورد: `002_cashflow.sql:114 permission denied for table instrument_fy1405`. پارتیشن‌هایی که `open_fiscal_year` با کاربر `postgres` می‌ساخت (db-init، آزمون‌ها) متعلق به `postgres` بودند، پس `GRANT … ON ALL TABLES` با کاربر `treasury_owner` خطا می‌داد. db-init در هر `docker compose up` دوباره اجرا می‌شود، در نتیجه مجموعه‌ای که متوقف شده بود دیگر بالا نمی‌آمد.
- اکنون `ensure_fiscal_year_partitions` دستور `ALTER TABLE … OWNER TO treasury_owner` را اجرا می‌کند. آزمون تازه `test_the_scripts_can_run_again_after_years_were_opened_by_an_administrator` دوباره `apply.sh` را اجرا و مالکان را بررسی می‌کند؛ بدون این اصلاح شکست می‌خورد.
- پایگاه‌های موجود که پارتیشنِ متعلق به `postgres` دارند یک بار به `ALTER TABLE … OWNER TO treasury_owner` نیاز دارند. بیرون از ماشین‌های آزمایشی چنین پایگاهی وجود ندارد.

**Docker Compose** (هر ۷ سرویس بالا، `db-init` با کد ۰ تمام می‌شود)

- `.dockerignore` (ریشه) و `frontend/.dockerignore` اضافه شد. بدون آن‌ها پوشه‌های `bin/obj` و `node_modules` میزبان به داخل ساخت فرستاده می‌شد.
- `gateway.conf`: ‏`Host $http_host` به‌همراه `X-Forwarded-Host`. با `$host` شماره پورت از دست می‌رفت: issuer در Keycloak به‌جای `:8080` می‌شد `http://localhost/auth/...`.
- پورت gateway اکنون به `${GATEWAY_ADDRESS:-127.0.0.1}` بسته می‌شود. API در حالت Development و بدون ورود اجرا می‌شود، پس پیش‌فرض نباید آن را در شبکه باز بگذارد. سرور پس از پیکربندی Keycloak مقدار `GATEWAY_ADDRESS=0.0.0.0` را می‌گذارد (در `.env.example` توضیح داده شده).
- `frontend/nginx.conf`: ‏`absolute_redirect off`. نشانی `/receipts` به `http://localhost/receipts/` هدایت می‌شد و پورت می‌افتاد.
- ریموت receipts: ‏`dts: false`. `tsc` ساده نمی‌تواند `.vue` را کامپایل کند، پس هر ساخت خطای `TYPE-001` ثبت می‌کرد، و هیچ‌چیز هم از آن typeها استفاده نمی‌کند. فایل ردیابی‌شده `.mf/diagnostics/latest.json` (گزارش ساخت همان خطا) حذف و `.mf/` نادیده گرفته شد.
- db-init دستور `bash /db/apply.sh` را صدا می‌زند (بدون تکیه بر بیت اجرایی در bind mount ویندوز). در `.gitattributes`: ‏`*.sh eol=lf`، و `*.cmd -text` که با CRLF ذخیره می‌شود.
- `deploy/smoke-test.sh`: شل، هدایت `/receipts`، manifest و entry در Module Federation، issuer در Keycloak (میزبان و پورت)، و دریافت ← خواندن ← واگذاری از راه gateway. این آزمون با `gateway.conf` قدیمی شکست می‌خورد. یک اجرای Playwright هم فرم دریافت را بدون خطای کنسول ثبت کرد.
- راه‌حل‌های موقتِ فقط برای محیط ابری، که **commit نشده‌اند** (در `docs/getting-started.md` شرح داده شده): imageهای پایه محلی با CA پراکسی، و Keycloak که از Docker Hub گرفته و با نام `quay.io/...` برچسب خورده، چون quay.io اینجا بسته است. اجراکننده‌های GitHub به هیچ‌کدام نیاز ندارند.

**سرور آزمایشی ویندوز** (`deploy/windows`، `docs/test-server-windows.md`)

- `treasury.ps1` (با نحو Windows PowerShell 5.1) با `start` / `stop` / `reset`، که سه فایل `.cmd` با پایان خط CRLF و گزینه `-ExecutionPolicy Bypass` آن را اجرا می‌کنند.
- تنظیمات یک بار در `%LOCALAPPDATA%\Treasury\treasury.env` ساخته می‌شود (بیرون از پوشه بازشده، پس به‌روزرسانی یعنی باز کردن ZIP تازه در هر جای دلخواه). نام پروژه compose یعنی `treasury`، ولوم `pgdata` را بین نسخه‌ها نگه می‌دارد.
- اسکریپت در صورت نیاز Docker Desktop را راه می‌اندازد و منتظر `docker info` می‌ماند. بررسی دستورهای بومی با `ErrorActionPreference=Continue` انجام می‌شود (وگرنه PS 5.1 خروجی stderr هدایت‌شده را به خطای متوقف‌کننده تبدیل می‌کند). صریحاً از `curl.exe` استفاده می‌کند (`curl` در PS 5.1 یک نام مستعار است). آماده بودن = پاسخ 404 از API برای شناسه ناشناخته + پاسخ 200 از شل؛ سپس مرورگر باز می‌شود.
- **آزموده شد** با PowerShell 7.6 روی لینوکس و روی مجموعه واقعی: شروع تازه، smoke test، توقف ← شروع با حفظ داده (همین، اشکال راه‌اندازی دوباره را پیدا کرد)، reset ردشده و تأییدشده، شروع خالی. اسکریپت بدون خطا parse می‌شود. **روی ویندوز واقعی آزموده نشد** (اینجا ماشین ویندوز نیست): فایل‌های `.cmd`، راه‌اندازی خودکار Docker Desktop و اجرای PS 5.1. نخستین اجرای مالک همان آزمون است.

**CI** (`.github/workflows/ci.yml`)

- job با نام `dotnet` یک سرویس `postgres:17` و متغیر `TREASURY_TEST_PG` دارد و `Treasury.Offline.slnf -p:Offline=true` را هم می‌سازد تا حالت آفلاین خراب نشود.
- job تازه `compose`: رمزهای دورریختنی، `up -d --build`، smoke test، **توقف ← up ← دوباره smoke test**، لاگ‌ها در صورت شکست، `down -v`.
- راه‌اندازها به `pull_request` و `push` روی `main` تغییر کرد (پیش‌تر هر push بود و هر PR دو بار اجرا می‌شد). لغو با `concurrency` اضافه شد.
- `tools/migration/pytest.ini` (`pythonpath = .`): `pytest` ساده نمی‌توانست `treasury_migration` را import کند، پس آن گام CI هرگز نمی‌توانست موفق شود.
- `pnpm/action-setup` اکنون `package_json_file: frontend/package.json` می‌گیرد. دنبال `package.json` در ریشه می‌گشت (وجود ندارد)، پس job فرانت‌اند پیش از نصب هر چیزی شکست می‌خورد.

**همچنین بررسی شد:** ساخت آفلاین و اجراهای shim همچنان موفق‌اند (۴۹ + ۱۲ + ۷). مستندات به‌روز شد: `docs/plan.md` (کارهای ۱ تا ۴ و ۶، فهرست انجام‌شده‌ها)، `docs/decisions.md`، جدول وضعیت README، ‏`docs/getting-started.md`، دستورهای CLAUDE.md. در `docs/domain` تغییری نبود، پس نسخه فارسی لازم نشد.

<a id="pr-2"></a>

### pull request شماره ۲

**بازبینی مالک از دریافت‌ها: ترتیب منوی اصلی و قاعده N2 (ثبت وصول یا برگشت پیش از سررسید ممنوع)**

ادغام‌شده، ۲۴ آبان ۱۴۰۵ · شاخه `claude/menu-and-clearing-date-rule` · https://github.com/BehinTaghavi/treasury/pull/2

- **Domain:** ‏`RecordCleared` و `RecordBounced` قاعده را از راه یک callback اختیاری تازه به نام `validate` روی `Apply` و با کمک‌تابع مشترک `RequireNotBeforeDueDate` بررسی می‌کنند. این بررسی پس از بررسی‌های گذار چرخه عمر، نوع سند و N1 و پیش از هر تغییر وضعیت اجرا می‌شود، پس گذار نامعتبر همچنان اول `Instrument.InvalidTransition` را گزارش می‌کند. اسنادی که سررسید ندارند (اقلام تضمینی) تأثیری نمی‌گیرند. چک‌های صادره (`RecordBouncedAtBank`، `RecordPaidByBank`) تغییری نکرده‌اند.
- **کدهای تازه:** ‏`Instrument.ClearedBeforeDueDate` و `Instrument.BouncedBeforeDueDate`، با متن فارسی و انگلیسی در `ui-kit/src/i18n.ts` (‏`codes.test.ts` آن را الزام می‌کند).
- **آزمون‌ها:** ۴ آزمون domain (وصول پیش از سررسید رد می‌شود و در روز سررسید پذیرفته؛ برگشت رد و پذیرفته می‌شود؛ گذار نامعتبر اول گزارش می‌شود). آزمون آداپتور برای وصول میان دو سال مالی اکنون سررسید واقع‌بینانه دارد (1405/01/10 به‌جای پیش‌فرض 1405/09/01 که N2 به‌درستی آن را رد می‌کرد).
- **مهاجرت داده:** ‏`map_cheque` وصول‌ها و برگشت‌های زودهنگام قدیمی را همان‌طور که ثبت شده‌اند نگه می‌دارد، با هشدار `… before the due date … (rule N2); kept as recorded`. ۲ آزمون تازه.
- **جدول چرخه عمر تغییری نکرده است.** این یک قاعده تاریخ است، نه گذار؛ پس `TRANSITIONS` در `legacy_mapping.py` و آزمون برابری دست‌نخورده‌اند.
- **شل:** ترتیب منو در `layouts/default.vue`؛ صفحه جانگهدار تازه `pages/payment-requests.vue` (از پیش رندرشده).
- **بررسی شد:** ۸۶ آزمون ‎.NET (شامل ۱۴ آزمون آداپتور روی PostgreSQL 17)، ۱۱ آزمون پایگاه داده، ۲۳ آزمون مهاجرت، ۷ آزمون UI-kit، ساخت شل. بررسی در مرورگر روی مجموعه Docker: ترتیب منو، و پیام فارسی برای وصول زودهنگام در حالی که چک در وضعیت `DepositedForCollection` می‌ماند.
- **مستندات:** تصمیم‌ها (۷ ردیف)، برنامه (فاز ب بازچینی شد: اول اطلاعات پایه)، پرسش Q11 پرسیده و پاسخ داده شد، قاعده‌های تاریخ N1/N2 در چرخه عمر، به انگلیسی و فارسی.

<a id="pr-3"></a>

### pull request شماره ۳

**اطلاعات پایه، گام ۱: ارزها، کشور/استان/شهر، نام بانک‌ها (reference-svc)**

ادغام‌شده، ۲۴ آبان ۱۴۰۵ · شاخه `claude/basic-info-reference-lists` · https://github.com/BehinTaghavi/treasury/pull/3

**سرویس تازه `reference-svc`** (`src/Services/Reference/`، ماژول طبق ADR 0001 با پروژه‌ها، شِما، نقش و outbox خودش):

- **Domain:** یک aggregate به نام `CatalogueItem` با `CatalogueKind` (Currency، Country، Province، City، Bank).
    - قاعده‌ها: نوع والد (Province←Country، ‏City←Province)، فعال بودن والد؛ کد ارز طبق ISO-4217، با حروف بزرگ و تغییرناپذیر؛ ریال (`IRR`) غیرفعال نمی‌شود؛ نام پاک‌سازی می‌شود و حداکثر ۱۰۰ نویسه است؛ غیرفعال‌کردن به‌جای حذف.
    - رویدادها: `CatalogueItemChanged`.
    - `PersianText` (در BuildingBlocks): ‏`Clean` (حذف فاصله‌های سر و ته، یکی‌کردن فاصله‌ها، حفظ نیم‌فاصله) و `Key` (تبدیل ي/ى به ی و ك به ک، رقم‌های فارسی/عربی به ASCII، حذف فاصله و نیم‌فاصله، حروف کوچک) برای مقایسه.
- **Application:** ‏`CatalogueUseCases` (Add، Rename، SetActive با نسخه مورد انتظار، List به ترتیب `fa-IR`).
    - همان الگوی cashflow: کلید idempotency، یک تراکنش، فرمان ناموفق چیزی ذخیره نمی‌کند، `Concurrency.Conflict` برای نسخه کهنه.
    - `Result<T>`، پورت‌ها و `ConcurrencyConflictException` از cashflow کپی شده‌اند، نه مشترک. یک هسته مشترک کوچک برای لایه Application می‌تواند بعداً بیاید.
- **فهرست‌های پیش‌فرض** (`DefaultLists.SeedAsync`، هنگام شروع API): فقط برای فهرستی که هیچ ردیفی ندارد، از راه use caseها و با کلیدهای idempotency ثابت، تا همان اعتبارسنجی، یکسان‌سازی و رویدادهای outbox ورودی‌های دستی را بگیرند. ابزار مهاجرت بعداً فهرست‌های خود مشتریان قدیمی را می‌آورد.
- **PostgreSQL** ‏`db/postgres/004_reference_catalogue.sql`:
    - جدول `reference.catalogue_item` با checkها: نوع، کد فقط برای ارز، والد فقط برای استان/شهر.
    - یکتایی `(kind, parent_id, name_key) NULLS NOT DISTINCT` و یکتایی کد ارز. جدول `reference.idempotency`. مجوزها به `reference_svc`.
    - آداپتور Npgsql نقض یکتایی ناشی از افزودن هم‌زمان را به `Catalogue.NameTaken` / `Catalogue.CodeTaken` تبدیل می‌کند.
- **API** ‏`/api/reference/{currencies|countries|provinces|cities|banks}`: ‏GET (‏`parentId`، `includeInactive`)، POST، ‏PUT `{id}`، ‏POST `{id}/activate|deactivate`. پاسخ problem details کد پایدار را دارد. اتصال Keycloak مثل cashflow است (ورود پس از اطلاعات پایه می‌آید).
- **استقرار:**
    - سرویس‌های `reference-api` و `remote-basic-info`؛ مسیرهای gateway برای `/api/reference/` و `/remotes/basic-info/`.
    - `REFERENCE_DB_PASSWORD` در `.env.example`، CI و db-init.
    - اسکریپت ویندوز اکنون **تنظیم‌های جاافتاده را به فایل تنظیمات موجود اضافه می‌کند** و هرگز تنظیم موجود را عوض نمی‌کند. با یک فایل قدیمی آزموده شد.
- **فرانت‌اند:** میکروفرانت‌اند تازه `remotes/basic-info` (‏`./BasicInfo` را عرضه می‌کند، `dts: false`)، با یک ویرایشگر فهرست مشترک که با `ListSpec` کار می‌کند.
    - ui-kit تابع `put()` و متن فارسی/انگلیسی ۱۲ کد تازه به‌علاوه `Concurrency.Conflict` را گرفت؛ `codes.test.ts` این را الزام می‌کند.
    - صفحه `basic-info` در شل، ریموت را بار می‌کند؛ ریموت‌های dev و پراکسی dev به‌روز شد.
- **آزمون‌ها:**
    - ۲۰ آزمون domain، ۱۳ آزمون use case (شامل اینکه پیش‌فرض‌ها یک بار و نه روی فهرست خود مشتری ریخته می‌شوند)، ۷ آزمون آداپتور PostgreSQL (با نقش `reference_svc`، شامل ۸ افزودن هم‌زمان یک بانک ← یک موفق، هفت `Catalogue.NameTaken`)، ۲ آزمون شِما، ۱ آزمون UI-kit.
    - معماری: قاعده‌های لایه‌ها برای همه سرویس‌ها بررسی می‌شود؛ هیچ سرویسی به سرویس دیگر ارجاع ندارد.
    - fixture مربوط به PostgreSQL به `tests/Support` رفت (به هر دو پروژه آزمون آداپتور link شده) و هنگام آماده‌سازی یک advisory lock در سطح سرور می‌گیرد، چون دو پروژه موازی اجرا می‌شوند و نقش‌های cluster مشترک است.
    - shim آزمون: ‏`Contains`/`DoesNotContain`/`Single` شرطی و `[InlineData(null)]`، تا ساخت آفلاین همچنان کامپایل شود.
- **بررسی محلی:**
    - ۱۲۸ آزمون ‎.NET (۰ ردشده)، ۱۳ آزمون پایگاه داده، ۲۳ مهاجرت، ۸ UI-kit؛ ساخت آفلاین به‌علاوه ۱۰۷ آزمون shim؛ ساخت فرانت‌اند.
    - مجموعه Docker از راه اسکریپت ویندوز با یک فایل تنظیمات قدیمی، به‌علاوه smoke test گسترش‌یافته.
    - مرور صفحه در مرورگر: افزودن، رد تکراری با حروف عربی، نیاز شهر به استان، خاموش‌کردن، رد ریال، انگلیسی.
- **در این PR نیست:** حساب‌های بانکی، رابط دسته‌چک، مراجع، گروه‌های بابت، صندوق‌ها؛ مهاجرت فهرست‌های قدیمی (`Bank`، `BasicBanks`، `City`).

<a id="pr-4"></a>

### pull request شماره ۴

**اطلاعات پایه، گام ۲: کدینگ حساب‌ها و تفصیل از سیستم حسابداری (ورود از Excel)**

ادغام‌شده، ۲۴ آبان ۱۴۰۵ · شاخه `claude/ledger-accounts-import` · https://github.com/BehinTaghavi/treasury/pull/4

- **Domain** (`Treasury.Reference.Domain/Ledger`):
    - `AccountCodeFormat`: طول کدها برای هر شرکت، که باید سطح به سطح بیشتر شود؛ سطح از روی طول کد؛ والد = پیشوند کد.
    - **`ChartOfAccounts`** و **`DetailAccountDirectory`** هر کدام یک aggregate واحدند، رونوشتی از ERP. هر ورود (import) یک فرمان، یک تراکنش و یک رویداد است.
    - `Import`/`Preview` این‌ها را پوشش می‌دهد: والد پیش از فرزند با هر ترتیبی در فایل؛ ردیف‌های با طول کد نادرست، تکراری، بدون والد یا بدون نام با شماره ردیف Excel رد می‌شوند؛ حساب‌هایی که در فایل نیستند غیرفعال و با بازگشت دوباره فعال می‌شوند؛ `IsPostable` = معین بدون فرزند فعال، یا جزء معین.
    - فایلی که هیچ ردیف پذیرفتنی ندارد رد می‌شود (`Ledger.NothingToImport`)، تا یک فایل اشتباه نتواند کدینگ را خاموش کند. طول کدها پس از وجود حساب قفل می‌شود.
    - **انواع مجاز تفصیل برای هر جایگاه یک مجموعه است** (مالک، ۱۹ آبان ۱۴۰۵ / <span dir="ltr">10 Nov 2026</span>). `SetDetailTypes` فقط روی حساب‌های قابل ثبت، انواع باید در فهرست تفصیل باشند، تفصیل ۲ به تفصیل ۱ نیاز دارد. `LedgerAccount.CheckDetails` کدهای `Ledger.DetailRequired` / `DetailNotAllowed` / `DetailTypeNotAllowed` را می‌دهد و برای حساب‌های بانکی و اسناد حسابداری آماده است.
- **Application:** ‏`LedgerUseCases` (قالب کد، پیش‌نمایش/ورود برای هر دو فهرست، انواع تفصیل، جست‌وجوی حساب و تفصیل از راه `ILedgerQueries`، جست‌وجو با سقف ۲۰۰).
    - `CommandRunner` مشترک تازه (idempotency، یک تراکنش، شکست چیزی ذخیره نمی‌کند)؛ use caseهای catalogue هم اکنون از آن استفاده می‌کنند.
    - پورت `ILedgerExportReader`.
- **Excel** (`Treasury.Reference.Infrastructure.Excel`، ‏ExcelDataReader 3.9.0 با مجوز MIT؛ در ساخت آفلاین نیست و آنجا ورود، `Excel.NotAvailable` برمی‌گرداند):
    - ستون‌ها از روی متن سرستون (کلیدهای PersianText) پیدا می‌شوند و ردیف‌های جداکننده/پانویس رد می‌شوند.
    - بازگشت به cp1256: خروجی‌های قدیمی رکورد CODEPAGE ندارند. کدها از عدد اعشاری (double) خوانده می‌شوند؛ رقم‌های فارسی یکسان می‌شوند.
    - کدهای روشن `Excel.UnknownLayout` / `Excel.Unreadable`.
- **PostgreSQL** ‏`005_reference_ledger.sql`:
    - جدول‌های سرآیند تک‌ردیفی (`ledger_chart`، `detail_directory`) نسخه aggregate را نگه می‌دارند.
    - `ledger_account` یک FK به خودش دارد (در سطح هر دستور بررسی می‌شود، پس والد و فرزند در یک upsert می‌روند) و ستون‌های `detail1_types`/`detail2_types` از نوع `text[]`. ‏`detail_account` ستون `name_key` برای جست‌وجوی غیرحساس به شکل حروف دارد.
    - فقط ردیف‌های تغییرکرده نوشته می‌شوند، با یک `INSERT … SELECT FROM unnest(…) ON CONFLICT` برای هر فهرست. مجموعه‌های آرایه متنی به شکل رشته‌هایی با جداکننده unit-separator فرستاده می‌شوند (آرایه تودرتو را نمی‌توان unnest کرد).
- **API** ‏`/api/reference/ledger/*`: قالب، `chart/import` و `details/import` (‏multipart، ‏`?preview=true`، ‏antiforgery برای این API مبتنی بر توکن خاموش است، سقف ۲۰ مگابایت)، حساب‌ها، `accounts/{code}/detail-types`، جست‌وجوی تفصیل، انواع تفصیل.
- **فرانت‌اند:** ‏`ImportBox` (پیش‌نمایش ← تأیید)، `ChartAccounts`، `DetailAccounts`؛ تابع `upload()` در ui-kit و متن فارسی/انگلیسی همه کدهای Ledger/Excel، شامل دلیل‌های رد در ورود.
- **آزمون‌ها:**
    - ۲۲ آزمون domain دفتر (قالب، درخت، دلیل‌ها به تفکیک ردیف، ورود دوباره، پیش‌نمایش، فایل خالی، قفل، مجموعه انواع مجاز و `CheckDetails`)، ۶ آزمون use case.
    - سناریوی PostgreSQL با نقش `reference_svc`: ده هزار تفصیل در یک تراکنش در حدود ۱ ثانیه، جست‌وجوی غیرحساس به شکل حروف، escape شدن `%`، رفت‌وبرگشت مجموعه انواع، حفظ انواع در ورود دوباره.
    - ۹ آزمون Excel: فایل‌های `.xls` **ساختگی** با همان چیدمان خروجی‌ها، ساخته‌شده با `Fixtures/make_fixtures.py`.
    - یک آزمون اختیاری `TREASURY_REAL_EXPORTS` که با فایل‌های مشتری اجرا کردم: ۰ ردیف ردشده. روی CI رد (skip) می‌شود.
    - ۱ آزمون UI-kit؛ ‏`Assert.All` در shim.
- **بررسی شد:**
    - ۱۶۷ آزمون ‎.NET (۱ ردشده = همان آزمون اختیاری با فایل‌های واقعی)، ۱۳ پایگاه داده، ۲۳ مهاجرت، ۹ UI-kit، ساخت آفلاین به‌علاوه آزمون‌های shim، ساخت فرانت‌اند.
    - مجموعه Docker از راه اسکریپت ویندوز روی پایگاه موجود (اسکریپت 005 هنگام به‌روزرسانی اعمال شد)، smoke test، مرور در مرورگر با فایل‌های واقعی.
- **در این PR نیست:** اتصال مستقیم به ERP (فاز F)؛ حساب‌های بانک (‎#5).
    - رونوشت مرجع تا وقتی `posting-svc` ساخته نشده در reference-svc می‌ماند. جابه‌جایی آن جابه‌جایی یک ماژول است، ADR 0001.

<a id="pr-5"></a>

### pull request شماره ۵

**اطلاعات پایه، گام ۳: حساب‌های بانکی شرکت (accounts-svc)**

ادغام‌شده، ۲۴ آبان ۱۴۰۵ · شاخه `claude/bank-accounts` · https://github.com/BehinTaghavi/treasury/pull/5

- **سرویس تازه `accounts-svc`** در `src/Services/Accounts` (‏Domain / Application / Infrastructure / Infrastructure.Postgres / Api)، در اجرای محلی روی پورت 5083.
    - شِمای خودش `accounts` و نقش خودش `accounts_svc` را دارد (`db/postgres/006_accounts.sql`)، به‌علاوه جدول‌های outbox و idempotency خودش.
    - هیچ کلید خارجی میان شِماها وجود ندارد.
- **ADR 0001:** بانک‌ها، شهرها، ارزها، حساب‌های دفتر و تفصیل از `reference-svc` و از راه HTTP خوانده می‌شوند (`IReferenceLookup` / `HttpReferenceLookup`).
    - به reference-svc این‌ها اضافه شد: ‏`GET /api/reference/{list}/{id}`، ‏`/ledger/accounts/{code}` و `/ledger/details/{code}`.
    - اگر reference-svc در دسترس نباشد، فرمان با `Reference.Unavailable` (HTTP 503) شکست می‌خورد. هیچ‌چیز نیمه‌بررسی‌شده ذخیره نمی‌شود.
- **Domain:** ‏aggregate به نام `BankAccount`.
    - قاعده‌ها از Access فرم `Form_Bankacc` و از MVC فایل `BanksController` آمده‌اند (قاعده‌های حساب پشتیبان، خط‌های ۱۱۴–۱۱۵ و ۲۴۹)؛ نام هر منبع در آزمون‌ها آمده است.
    - یکتایی با `number_key` فقط‌رقمی سنجیده می‌شود، پس رقم فارسی و لاتین برابر حساب می‌شوند.
    - پشتیبان در پایگاه داده: قید UNIQUE روی `number_key` و ایندکس یکتای جزئی `bank_account_one_backup`، که هر دو در unit of work به کد پایدار برگردانده می‌شوند.
- **`FiscalDate` به BuildingBlocks.Domain منتقل شد،** تا همچنان فقط یک مفسر تاریخ جلالی وجود داشته باشد. در Cashflow فقط خط‌های `using` عوض شده است.
- **استقرار:**
    - سرویس compose به نام `accounts-api`، با مسیر gateway برای `/api/accounts/`؛
    - `ACCOUNTS_DB_PASSWORD` در `.env.example`، CI و اسکریپت ویندوز. اسکریپت تنظیم جاافتاده را به فایل تنظیمات موجود اضافه می‌کند؛ این را روی یک فایل قدیمی بررسی کردم و پایگاه موجود، نقش و شِمای تازه را گرفت؛
    - `smoke-test.sh` نشانی تازه را بررسی می‌کند.
- **آزمون‌ها:**
    - ۱۵ آزمون domain + ‏۱۳ آزمون use case + ‏۵ آزمون آداپتور PostgreSQL؛
    - یک آزمون شِما که ثابت می‌کند `accounts_svc` هیچ شِمای دیگری را نمی‌بیند؛
    - آزمون‌های معماری اکنون Accounts را هم پوشش می‌دهند؛
    - هر ۳۱ کد خطای تازه در `i18n.ts` متن فارسی و انگلیسی دارند.
- **اجرای کامل:** ۲۰۱ آزمون ‎.NET (۲۰۰ موفق، ۱ ردشده)، ۱۴ آزمون پایگاه داده، ۲۳ آزمون مهاجرت، ۹ آزمون UI-kit و ساخت فرانت‌اند همه موفق‌اند. ساخت آفلاین ۰ هشدار دارد و اجراهای shim برای Accounts موفق است. smoke test در Docker و بررسی مرورگر با Playwright هم موفق است.
- **هنوز انجام نشده:**
    - ورود (login) نیست؛ نشانی‌ها باز هستند، مثل سرویس‌های دیگر تا زمان ورود در فاز ب؛
    - مانده‌ها، و پیوند دسته‌چک به حساب بانکی، بعداً می‌آیند.
- **ادامه کار:** بلوک مشترک لایه Application در pull request شماره ۶ است.

<a id="pr-6"></a>

### pull request شماره ۶

**مشترک‌کردن زیرساخت اجرای فرمان در سه سرویس (بدون تغییر رفتار)**

ادغام‌شده، ۲۴ آبان ۱۴۰۵ · شاخه `claude/shared-application-block` · https://github.com/BehinTaghavi/treasury/pull/6

- **پروژه تازه `src/BuildingBlocks/Treasury.BuildingBlocks.Application`** (فضای نام `Treasury.BuildingBlocks.Application`). فقط به `Treasury.BuildingBlocks.Domain` ارجاع دارد و این‌ها را در بر می‌گیرد:
    - `Result<T>`: نسخه cashflow با `Warnings`، که دو نسخه دیگر را هم پوشش می‌دهد؛
    - `IUnitOfWork`، `IIdempotencyStore` و `ConcurrencyConflictException`؛
    - `CommandRunner`: بررسی کلید idempotency، یک تراکنش، `Result` ناموفق تراکنش را لغو می‌کند (همراه با رکورد idempotency)، تبدیل `DomainException` به نتیجه، و تبدیل `ConcurrencyConflictException` به `Concurrency.Conflict`.
- **حذف شد:** نسخه‌های کپی در پوشه‌های `Application/Abstractions` سرویس‌های Accounts و Reference، و نوع‌های تکراری در `Ports.cs` سرویس Cashflow. پورت‌های ویژه هر سرویس سر جای خود مانده‌اند.
- **Cashflow:** ‏`InstrumentUseCases` به‌جای `RunAsync`/`CommandFailedException` خصوصی خود از `CommandRunner` مشترک استفاده می‌کند. منطق همان است: این همان کدی است که دو سرویس دیگر از روی آن کپی شده بودند.
- **بقیه تغییرها** فقط خط‌های `using` است، به‌علاوه ورودی‌های solution در `Treasury.slnx` و `Treasury.Offline.slnf`. ‏Dockerfileها کل `src/` را کپی می‌کنند، پس تغییری نکرده‌اند.
- **آزمون‌های معماری (۴ آزمون تازه):**
    - بلوک مشترک فقط به `BuildingBlocks.Domain` و `System.*` ارجاع دارد؛
    - assembly لایه Application هر سه سرویس به آن ارجاع دارد.
- `docs/architecture.md` هر دو building block را شرح می‌دهد و می‌گوید قاعده کسب‌وکار هرگز آنجا نمی‌رود.
- **بررسی شد:**
    - ۲۰۵ آزمون ‎.NET (۲۰۴ موفق، ۱ ردشده مثل قبل)، شامل آزمون‌های آداپتور روی PostgreSQL 17؛
    - ساخت آفلاین ۰ هشدار دارد؛
    - اجراهای shim برای آزمون‌های Application سه سرویس Cashflow، ‏Reference و Accounts و برای آزمون‌های معماری.
    - پایگاه داده، مهاجرت و فرانت‌اند دست‌نخورده‌اند؛ CI آن‌ها را اجرا می‌کند.
- **ریسک:** کم. تغییر خود این pull request شامل ۳۷ فایل با ۸۱ خط افزوده و ۲۲۱ خط حذف‌شده است، همه مکانیکی، و قراردادهای API تغییری نکرده‌اند.

<a id="pr-7"></a>

### pull request شماره ۷

**مستندات: راهنمای بازبین به فارسی**

ادغام‌شده، ۲۴ آبان ۱۴۰۵ · شاخه `claude/wonderful-mccarthy-e19bxy` · https://github.com/BehinTaghavi/treasury/pull/7

- فقط مستندات: یک فایل تازه، یک جمله در `README.md` («از اینجا شروع کنید») و یک ردیف در فهرست انجام‌شده‌های `docs/plan.md`. بدون تغییر در کد، شِما یا آزمون.
- راهنما کد را در وضعیت ۲۴ آبان ۱۴۰۵ (<span dir="ltr">15 Nov 2026</span>) شرح می‌دهد و می‌گوید چه چیزهایی هنوز ساخته نشده (ورود با Keycloak، منتشرکننده outbox، چهار سرویس بعدی، و جایگزین‌های موقت `DevelopmentPolicies` در cashflow-svc).
- بررسی شد: هر مسیر مخزن و هر نام کلاس، متد، آزمون و کد خطا که در راهنما آمده در مخزن وجود دارد (بررسی با اسکریپت). صفحه به HTML تبدیل شد تا چیدمان راست‌به‌چپ، ۲۱ جدول، چک‌لیست و دو نمودار چپ‌به‌راست تأیید شود.
- بررسی نشد: پیوندهای بیرونی باز نشدند. به صفحه نخست سایت‌های مستندات رسمی اشاره می‌کنند، همراه با عنوان صفحه برای جست‌وجو.
- به درخواست مالک فقط فارسی است. `docs/decisions.md` می‌گوید نسخه مرجع مستندات انگلیسی است؛ این راهنما نسخه انگلیسی ندارد. اگر لازم باشد، می‌تواند در pull request جداگانه بیاید.
- راهنما یک تصویر لحظه‌ای است: هر وقت الگوهایی که شرح می‌دهد عوض شود باید به‌روز شود (در سطرهای نخستش همین را گفته است).

<a id="pr-8"></a>

### pull request شماره ۸

**کار ۲۰ تا ۲۴ آبان (<span dir="ltr">11–15 Nov</span>): تکمیل اطلاعات پایه، موجودی اول دوره، دریافت‌ها، درخواست‌های پرداخت، اسناد حسابداری، مراحل چک**

ادغام‌شده، ۲۴ آبان ۱۴۰۵ · شاخه `claude/nifty-newton-d1cong` · https://github.com/BehinTaghavi/treasury/pull/8

**چرا یک pull request بزرگ.** این PR شامل ۴۵ commit، ‏۲۵۱ فایل و حدود ۲۴٬۶۰۰ خط افزوده است، بسیار بیشتر از قاعده «هر pull request یک کار». commitها روی هم ساخته شده‌اند (دریافت‌ها روی تغییرهای اطلاعات پایه، درخواست‌های پرداخت و اسناد حسابداری روی دریافت‌ها، مراحل چک روی اسناد حسابداری)، پس به‌جای برش دوباره، یک‌جا فرستاده شده است. commitها موضوعی و به‌ترتیب‌اند؛ راه عملی، بازبینی commit به commit است.

**CI هنوز روی هیچ‌کدام از این‌ها اجرا نشده است.** workflow فقط روی pull request و push به `main` اجرا می‌شود، پس این pull request نخستین باری است که این commitها از آن می‌گذرند. آزمون‌ها هنگام باز کردن PR دوباره اجرا نشدند. اگر شکستی باشد روی همین شاخه اصلاح می‌شود.

**شاخه ۴ commit ادغام از `main` عقب است** (commitهای ادغام pull requestهای ۱، ۲، ۵ و ۶). پایه آن نوک pull request شماره ۶ است، پس محتوا یکی است؛ ادغام آزمایشی در `main` بدون تعارض است.

چه چیزهایی را نگاه کنید:

- **سرویس تازه** ‏`src/Services/Requests` (‏`requests-svc`: ‏Domain، Application، Infrastructure، Infrastructure.Postgres، Api) با سه پروژه آزمون، نقش پایگاه داده `requests_svc` و میکروفرانت‌اند `frontend/remotes/requests`. به Docker Compose، ‏gateway، ‏CI، ‏smoke test و اسکریپت شروع ویندوز وصل شده است. `deploy/.env.example` متغیر `REQUESTS_DB_PASSWORD` را گرفته (با مقدار جانگهدار).
- **میکروفرانت‌اند تازه** ‏`frontend/remotes/receipts`؛ افزوده‌های زیاد به `frontend/remotes/basic-info` و منوی شل.
- **اسکریپت‌های شِما از 007 تا 032** در `db/postgres` (‏reference، accounts، cashflow، requests)، به‌علاوه تغییر در `000_roles.sql` و `db/postgres/tests/test_schema.py`.
- **`InstrumentLifecycle.cs` تغییر کرده است** (تنها مرجع تغییر وضعیت‌های مجاز)، با تغییر متناظر در `tools/migration/treasury_migration/legacy_mapping.py` و در `docs/domain/instrument-lifecycle.md` / `.fa.md`. طبق قاعده‌های پروژه این یک تصمیم کسب‌وکار است؛ پاسخ‌های مالک که پشتوانه آن است Q30 تا Q36 هستند.
- **Domain سرویس Cashflow**: ‏`Receipt`، اسناد حسابداری، مراحل چک، قالب‌های چک، با آزمون‌های domain، ‏use case و PostgreSQL.
- **`frontend/packages/ui-kit/src/i18n.ts`**: حدود ۵۷۰ خط افزوده متن فارسی و انگلیسی برای کدهای خطا و صفحه‌های تازه.
- **مستندات**: ‏`docs/decisions.md` (تصمیم‌های مالک از ۲۰ تا ۲۴ آبان)، `docs/plan.md`، ‏`docs/domain/open-questions.md` (‏Q12 تا Q36)، `posting-rules.md`، فایل‌های تازه `cheque-printing.md` و `received-cheque-steps.md`، هر کدام با نسخه فارسی؛ یک افزوده به `docs/adr/0002-one-transaction-per-command.md`.
- **ارجاع به بسته‌های تازه**: ‏`Npgsql 10.0.3`، ‏`Microsoft.AspNetCore.Authentication.JwtBearer 10.0.0`، ‏`Microsoft.Extensions.DependencyInjection.Abstractions 10.0.0` (برای سرویس تازه)؛ ریموت‌های تازه از همان بسته‌های فرانت‌اند ریموت‌های موجود استفاده می‌کنند. هیچ‌کدام در فهرست مجوزهای تجاری ممنوع نیست.

هنگام باز کردن این pull request بررسی شد: diff هیچ فایل `.mdb`، پشتیبان، Excel، آرشیو یا کلید و هیچ فایل باینری اضافه نمی‌کند، و جست‌وجوی الگو در خط‌های افزوده هیچ مقدار رمز یا کلیدی پیدا نکرد.

در این pull request نیست: راهنمای بازبین به فارسی (pull request شماره ۷) و صفحه اصلی فارسی README (شاخه `claude/charming-feynman-obr5jn`، که پس از ادغام این PR به یک اصلاح کوچک در `docs/decisions.md` و `docs/plan.md` نیاز دارد).

<a id="pr-9"></a>

### pull request شماره ۹

**مستندات: صفحه اصلی فارسی و راهنمای ساخت و اجرای بازبین، هم‌گام با main امروز**

ادغام‌شده، ۲۴ آبان ۱۴۰۵ · شاخه `claude/eloquent-johnson-jflwpx` · https://github.com/BehinTaghavi/treasury/pull/9

- فقط مستندات: بدون تغییر در کد، شِما یا آزمون.
- **شاخه‌های منبع.** ‏`claude/charming-feynman-obr5jn` (README) و `claude/magical-lovelace-xt2v4e` (راهنما) با تاریخچه‌شان اینجا ادغام شده‌اند. در تعارض با pull requestهای ۷ و ۸ در `docs/plan.md`، ‏`docs/decisions.md` و `docs/architecture.md` هر دو طرف نگه داشته شده است.
- **دو راهنمای بازبین، دو نام فایل.** شاخه دوم راهنمایش را با نام `docs/reviewer-guide.fa.md` اضافه کرده بود؛ همان نامی که pull request شماره ۷ برای راهنمای کد و بازبینی به کار برده است. راهنمای ساخت و اجرا با نام `docs/reviewer-run-guide.fa.md` نگه داشته شد؛ پیوندها در `CLAUDE.md`، ‏`docs/getting-started.md`، ‏`docs/plan.md` و هر دو README اصلاح شد.
- **هم‌گام با `main` پس از pull request شماره ۸:**
    - README (هر دو زبان): آخرین تغییرها (pull requestهای ۷ و ۸)، وضعیت فعلی (چهار سرویس، سه میکروفرانت‌اند)، گام‌های بعدی، چیدمان مخزن، نقطه‌های شروع بازبین.
    - راهنمای اجرا: دوازده سرویس Compose به‌جای ده (`requests-api`، `remote-requests`)، چهار API در smoke test، منو با «موجودی اول دوره»، اینکه هر گزینه منو چه نشان می‌دهد، `/api/requests/`. برگرفته از `deploy/docker-compose.yml`، ‏`deploy/gateway.conf`، ‏`deploy/smoke-test.sh` و منوی شل.
    - `CLAUDE.md`: سطر «Where things are» برای سرویس‌ها، و دو قاعده README/راهنما که شاخه‌ها اضافه کرده بودند.
- **شمار آزمون‌ها.** شمار آزمون به تفکیک پروژه در README مربوط به شش pull request پیش بود. به‌جای آن، نوع آزمون‌های هر بخش و یک جمع کل از اجرای CI روی pull request شماره ۸ آمده است: ۵۱۰ آزمون ‎.NET، ‏۵۰۹ موفق، ۱ ردشده (فقط به‌صورت محلی و با فایل‌های واقعی مشتری اجرا می‌شود). شمار آزمون‌های پایگاه داده، مهاجرت و ui-kit دیگر ذکر نمی‌شود.
- **دوباره با دست اجرا نشد.** دستورهای راهنمای اجرا را همان نشستی که آن را نوشت از یک clone تازه و روی `main` پیشین اجرا کرده بود. پس از این به‌روزرسانی، اینجا دوباره اجرا نشدند؛ job با نام `compose` در CI همان ساخت و smoke test را روی این pull request اجرا می‌کند.
- **در گام‌های بعدی README آمده است:** pull request شماره ۸ به دستور مالک و بدون ثبت تأیید بازبین ادغام شد، پس بازبینی آن کار هنوز باقی است.
- ستون وضعیت فازهای ب تا د در `docs/plan.md` بازنویسی نشد؛ فهرست انجام‌شده‌ها به‌روز است.

<a id="pr-10"></a>

### pull request شماره ۱۰

**سفته: فرم دریافت با رسید چاپی، مراحل پس از دریافت، و صاحبان امضای مجاز**

باز از ۲۵ آبان ۱۴۰۵، CI سبز · شاخه `claude/elegant-ramanujan-58j452` · https://github.com/BehinTaghavi/treasury/pull/10

**اندازه.** این PR از pull request معمول ما بزرگ‌تر است: مالک هر سه بخش را پشت سر هم و در یک نشست خواست. commitها جداشدنی‌اند و می‌توان به ترتیب بازبینی کرد: دریافت سفته ← پاسخ‌های مالک ← صاحبان امضای مجاز ← مراحل سفته ← پاسخ‌های مالک (آخرین commit فقط پیوند این pull request را در صفحه‌های اصلی می‌گذارد).

**Domain سرویس Cashflow**

- `ReceiptKind.Notes` با `NoteReceiptTerms` (علت، شکل، سررسید، نگهداری، دریافت اصل، پیوست‌ها، امضاکنندگان). `Receipt.RecordNotes` سفته‌ها را با این شرایط، با بررسی‌های ماده ۳۰۸ و با قاعده امضا می‌سنجد.
- به `ReceivedInstrumentDetails` فیلدهای `WrittenOn`، ‏`OnDemand` و `Checks` اضافه شد؛ به `NoteGuarantor` فیلدهای `DetailCode` و `Phone`. همه اختیاری‌اند، پس JSON ذخیره‌شده قبلی همچنان خوانده می‌شود. `MaxGuarantors` اکنون ۳ است (پاسخ مالک؛ راهنمای قدیمی دو گفته بود).
- سفته عندالمطالبه سررسید ندارد (`Instrument.OnDemandHasDueDate`)؛ سررسید سفته تاریخ‌دار پیش از تاریخ نوشته‌شده روی آن نیست.
- **چرخه عمر:** هیچ وضعیت یا مرحله‌ای اضافه نشد، پس نسخه ابزار مهاجرت تغییری نکرده و آزمون مقایسه آن موفق است. فقط نوع‌ها تغییر کرده‌اند: `IsNegotiable` اجازه می‌دهد سفته عادی به بانک واگذار و ظهرنویسی شود (قاعده قدیمی R3 فقط چک عادی و بانکی را مجاز می‌دانست). `Endorse` شماره رهگیری صیاد را فقط برای چک می‌خواهد.
- **اسناد حسابداری:** دو حساب ثابت تازه، `GuaranteeNotesForCompany` و `NotesInCollection`. پس‌دادن سفته تضمینی، ثبت انتظامی را برمی‌گرداند. پس‌دادن سفته عادی، معینِ گروه بابتِ رسید آن را بدهکار می‌کند (در `VoucherUseCases.ComputeStepAsync` پیدا می‌شود). چکی که به پرداخت‌کننده‌اش برگردانده می‌شود مثل قبل سند حسابداری ندارد.

**سرویس Reference**

- aggregateهای تازه `Signatory` و `SignatureRule`، ‏`SignatureUseCases`، مخزن‌های درون‌حافظه‌ای و PostgreSQL، نشانی‌های `/api/reference/signatories` و `/api/reference/signature-rules`.
- Cashflow قاعده را از راه HTTP می‌خواند (`IReceiptReferenceLookup.SignatureRuleAsync`) و انتخاب را در domain خودش بررسی می‌کند (`SignatureRuleFact.Choose`) و نام و سمت امضاکنندگان را روی رسید نگه می‌دارد.

**پایگاه داده:** اسکریپت‌های 033 (نوع `Notes`، ‏`note_terms`، حساب ثابت)، 034 (`reference.signatory`، ‏`reference.signature_rule`)، 035 (حساب ثابت). اسکریپت 033 قیدهای check نوع را با پیدا کردن آن‌ها از روی تعریف جایگزین می‌کند، پس اجرای دوباره‌اش بی‌خطر است؛ `apply.sh` را دو بار اجرا کردم.

**فرانت‌اند**

- `NoteReceiptForm.vue`، ‏`NoteReceiptSheet.vue` / `NoteReceiptPrint.vue`، ‏`notes.ts`؛ ‏`Signatories.vue` در basic-info.
- `ChequeOperations.vue` یک prop به نام `notes` می‌گیرد و واژه‌ها را با یک کمک‌تابع عوض می‌کند، به‌جای ساختن نسخه دوم صفحه. ارزش نگاه کردن دارد: این جایگزینی با تعویض متن انجام می‌شود.
- چاپ، یک نسخه از برگه را زیر `<body>` می‌برد (teleport) و بقیه را با یک قاعده `@media print` غیر scoped پنهان می‌کند.

**آزمون‌ها:** CI در هر چهار job سبز است (`dotnet`، ‏`database-and-migration`، ‏`frontend`، ‏`compose`). محلی: ۵۷۵ آزمون ‎.NET (۵۷۴ موفق، ۱ ردشده مثل `main`)، ۳۹ آزمون شِما، ۲۳ آزمون مهاجرت، آزمون‌های ui-kit، ساخت آفلاین. تازه‌ها: `NoteReceiptTests`، ‏`NoteStepTests`، ‏`SignatureTests`، ‏`NoteReceiptUseCaseTests`، ‏`SignatureUseCaseTests`، ‏`SignaturePostgresTests`، و افزوده‌هایی به آزمون‌های سند حسابداری، رسید و شِما.

**بررسی دستی** در مرورگر روی سرویس‌های درون‌حافظه‌ای: ذخیره و چاپ رسید سفته (یک صفحه A4)، صفحه صاحبان امضا و قاعده آن، واگذاری دو سفته با یک فیش، رد وصول پیش از سررسید، وصول سفته عندالمطالبه، پس‌دادن سفته تضمینی.

**انجام‌نشده یا اجرانشده**

- Docker Compose و `deploy/smoke-test.sh` با دست اجرا نشدند؛ فقط job با نام `compose` در CI آن‌ها را اجرا کرد. صفحه‌های تازه در ساخت Docker دیده نشده‌اند.
- PostgreSQL محلی نسخه 16 بود؛ CI آزمون‌های شِما و آداپتور را روی 17 اجرا کرد.
- ظهرنویسی سفته فقط با آزمون‌های use case پوشش داده شد، نه در مرورگر (به requests-svc و یک درخواست تأییدشده نیاز دارد).
- سند حسابداری پس‌دادن سفته عادی پس از بررسی مرورگر اضافه شد؛ فقط با آزمون‌های domain و use case پوشش دارد.
- پیوست‌ها فقط تیک هستند، بدون بارگذاری فایل (Q43).
- رسید چاپی جای نام شرکت را نقطه‌چین می‌گذارد: مشخصات شرکت هنوز در اطلاعات پایه نگهداری نمی‌شود.
- API دریافت چک هنوز مثل قبل سفته را می‌پذیرد؛ فقط صفحه‌ها آن‌ها را جدا می‌کنند.
- قاعده‌های امضای پنج سند دیگر ذخیره می‌شوند ولی هنوز استفاده نمی‌شوند.

<a id="pr-11"></a>

### pull request شماره ۱۱

**«ثبت صیادی شد»: چک فقط پس از تیک «ثبت در صیاد» به بانک می‌رود**

باز از ۲۶ آبان ۱۴۰۵، CI سبز · شاخه `claude/sleepy-rubin-6znv1v` · https://github.com/BehinTaghavi/treasury/pull/11

**طراحی**

- تیک، یک ویژگی `Instrument` است (`SayadRegistration`: لحظه و کاربر)، نه وضعیت و نه مرحله تاریخچه. `InstrumentLifecycle.cs` تغییری نکرده، پس نسخه ابزار مهاجرت همچنان با آن می‌خواند.
- `DepositForCollection` (هر دو overload) و `Deliver` وقتی چک تیک ندارد با `Instrument.SayadNotRegistered` رد می‌کنند. `Instrument.AllowedActions()` تا آن زمان عمل محافظت‌شده را در فهرست نمی‌آورد، پس صفحه به قاعده‌ای از خودش نیاز ندارد.
- `ConfirmSayadRegistration` / `WithdrawSayadRegistration` فقط تا وقتی مجازند که مرحله محافظت‌شده هنوز پیش رو باشد (`CanChangeSayadRegistration`). هر کدام رویداد `InstrumentSayadRegistrationChanged` را به outbox می‌فرستد؛ همین، ردِ «چه کسی و چه زمانی تیک زد» است.
- فرمان تازه `SetSayadRegistrationAsync` (کلید idempotency، یک aggregate که یک بار ذخیره می‌شود، ساعت از `TimeProvider`) و `POST /api/cashflow/instruments/{id}/sayad-registration`. ‏`InstrumentUseCases` اکنون یک `TimeProvider` می‌گیرد.
- شِما `033_cashflow_sayad_registration.sql`: ‏`sayad_registered_at timestamptz`، ‏`sayad_registered_by text`، و یک check که یا هر دو مقدار دارند یا هر دو null هستند.
- پاسخ Q39: اسکریپت 033 چک‌های دریافتی موجود را **فقط هنگامی که ستون‌ها را می‌سازد** تیک می‌زند، پس اجرای دوباره `apply.sh` چیزی را تیک نمی‌زند. ابزار مهاجرت هم برای چک‌های دریافتی مهاجرت‌شده همین کار را می‌کند. هر دو به‌جای کاربر `system:assumed` می‌نویسند (`SayadRegistration.AssumedBy`)؛ اگر این نشانه یا نوع‌های چک میان domain، اسکریپت و ابزار فرق کند یک آزمون مهاجرت شکست می‌خورد.
- فرانت‌اند: ‏`formatJalaliDateTime` در ui-kit (تاریخ و ساعت جلالی در `Asia/Tehran`)، چهار کد خطای تازه در `i18n.ts` (فارسی و انگلیسی)، و ستون، راهنما و سطر کارت در `ChequeOperations.vue`.
- `deploy/smoke-test.sh` یک چک را بدون تیک واگذار می‌کرد و شکست می‌خورد؛ اکنون رد شدن (422) را بررسی می‌کند، تیک می‌زند، سپس واگذار می‌کند.

**آزمون‌ها**

- Domain: ‏`SayadRegistrationTests` (چک روز و مدت‌دار، برداشتن تیک، برگشت از بانک، سفته، چک صادره، تیک فرضی، ظهرنویسی بدون تیک). آزمون‌های موجود که واگذار یا تحویل می‌کنند اکنون اول تیک می‌زنند.
- Use caseها و آداپتور PostgreSQL: رد شدن، تیک با لحظه و کاربر، برداشتن، رویدادهای outbox، بازخوانی تیک فرضی.
- شِما: ستون‌ها و check آن‌ها؛ تیک یک‌باره چک‌های موجود و اینکه اجرای دوم چیزی را تیک نمی‌زند.
- مهاجرت: چک‌های دریافتی مهاجرت‌شده تیک می‌خورند، چک‌های صادره و سفته‌ها نه.
- اجرای محلی: ۵۲۶ آزمون ‎.NET موفق و ۱ ردشده (به فایل‌های واقعی مشتری نیاز دارد)، ۳۹ آزمون شِما، ۲۴ آزمون مهاجرت، ۱۳ آزمون ui-kit؛ `pnpm build` موفق است. صفحه در مرورگر با داده ساختگی آزموده شد (دریافت، تیک، برداشتن، واگذاری، کارت).

**اینجا بررسی نشد**

- پایگاه داده محلی PostgreSQL 16 بود، نه 17.
- Docker در این محیط کار در دسترس نیست، پس کل سیستم در Compose و `smoke-test.sh` تغییریافته سرتاسری اجرا نشدند؛ سه فراخوانی تازه آن روی API درون‌حافظه‌ای امتحان شد. job با نام `compose` در CI بررسی واقعی است.
- متن «از قبل ثبت‌شده فرض شد» در مرورگر دیده نشد.

**برای بعد**

- جعبه تیک روی فرم پرداخت، پیش از «تحویل چک به ذینفع» (فاز E).
- اینکه چه کسی می‌تواند تیک بزند (مجوزها) همراه با ورود Keycloak می‌آید.

<a id="pr-12"></a>

### pull request شماره ۱۲

**«اطلاعات شرکت»: نام و نشان شرکت بالای همه صفحه‌ها، همراه با مشخصات رسمی**

باز از ۲۶ آبان ۱۴۰۵، CI سبز · شاخه `claude/wonderful-mccarthy-tr7zvt` · https://github.com/BehinTaghavi/treasury/pull/12

**طراحی**

- `reference-svc`: ‏aggregate تازه `CompanyProfile` (‏`Domain/Company`) با رکورد `CompanyDetails`؛ یک پروفایل برای هر پایگاه داده مشتری. Use caseهای `CompanyUseCases` (ذخیره فرم، پیوست نشان، حذف نشان، خواندن)، آداپتورهای درون‌حافظه‌ای و PostgreSQL، نشانی‌ها زیر `/api/reference/company` (‏`GET`، ‏`PUT`، ‏`POST /logo`، ‏`POST /logo/remove`، ‏`GET /logo`).
- نسخه ۰ یعنی «هنوز وارد نشده»؛ نخستین ذخیره، پروفایل را ثبت می‌کند و ذخیره‌های بعدی نسخه را بررسی می‌کنند. نخستین ورود هم‌زمان را جدول (`company_single`) متوقف می‌کند و به شکل `Concurrency.Conflict` گزارش می‌شود.
- شِما `033_reference_company.sql` (جدول تک‌ردیفی، نشان در جدول جداگانه تا خواندن نام هرگز تصویر را بار نکند) و `034_reference_company_details.sql` (‏`ADD COLUMN IF NOT EXISTS`، هر ستون با check شکل). نقش `reference_svc` اجازه حذف پروفایل را ندارد.
- بررسی شناسه ملی از `Beneficiary.IsLegalNationalId` استفاده می‌کند.
- فرانت‌اند: ‏`CompanyInfo.vue` در ریموت basic-info؛ ‏composable به نام `useCompany` در شل و نوار بالا. ریموت، تغییر را با رویداد پنجره `treasury:company-changed` به شل خبر می‌دهد (ثابت در `@treasury/ui-kit`، فایل `company.ts`)، پس هیچ‌کدام دیگری را import نمی‌کند. نشان با fetch گرفته و از یک blob URL نشان داده می‌شود، نه `<img src>`، تا پس از اتصال Keycloak درخواست بتواند توکن را همراه داشته باشد.
- ۱۳ کد خطای تازه `Company.*`، با متن فارسی و انگلیسی.

**آزمون‌ها**

- Domain (‏`CompanyProfileTests`)، ‏use caseها (`CompanyUseCaseTests`)، آداپتور PostgreSQL (‏`CompanyPostgresTests`)، شِما (`test_one_company_profile_with_a_small_logo`)، ‏ui-kit (‏`logoFileProblem`)، و یک خط در `deploy/smoke-test.sh`.
- اجرای محلی: ۵۲۴ آزمون ‎.NET موفق و ۱ ردشده (همان آزمون موجود با فایل‌های واقعی)؛ ۳۸ آزمون شِما؛ ۱۴ آزمون ui-kit؛ ساخت آفلاین و دو پروژه آزمون آفلاین Reference.
- بررسی مرورگر روی API درون‌حافظه‌ای: ذخیره نام و نشان، دیدن آن‌ها در صفحه‌های دیگر و پس از بارگذاری دوباره، رد PDF، رد شناسه ملی و نشانی وب‌سایت نادرست با پیام فارسی، حذف نشان.

**انجام‌نشده / قابل توجه**

- PostgreSQL محلی نسخه 16 بود، نه 17؛ CI آزمون‌های آداپتور و شِما را روی 17 اجرا می‌کند.
- اجرای کامل Docker Compose به‌صورت محلی انجام نشد؛ job با نام `compose` آن را پوشش می‌دهد.
- درخواست‌های نوار بالا هنوز توکن ندارند، مثل بقیه شل (ورود Keycloak بعداً در فاز ب می‌آید).
- **شماره‌گذاری با دو pull request باز دیگر هم‌پوشانی دارد.** شماره ۱۰ اسکریپت‌های شِمای 033–035 و پرسش‌های Q37–Q52 را اضافه می‌کند؛ شماره ۱۱ اسکریپت 033 و پرسش‌های Q37–Q39 را؛ این یکی اسکریپت‌های 033–034 و پرسش‌های Q37–Q38 را. اسکریپت‌های هم‌شماره با نام متفاوت همچنان به ترتیب اعمال می‌شوند، ولی شماره پرسش‌ها اکنون سه معنای متفاوت دارد. هر کدام که دیرتر ادغام شود باید پرسش‌هایش (و ترجیحاً اسکریپت‌هایش) را دوباره شماره‌گذاری کند و در `README.md`، ‏`README.en.md`، ‏`docs/decisions.md` و `docs/domain/open-questions.md` تعارض متنی خواهد داشت. این pull request امروز بدون تعارض در `main` ادغام می‌شود.

<a id="pr-13"></a>

### pull request شماره ۱۳

**نمایش نسخه در حال اجرا بالای صفحه و در پنجره «Start Treasury»**

باز از ۲۶ آبان ۱۴۰۵، CI سبز · شاخه `claude/nifty-newton-d1cong` · https://github.com/BehinTaghavi/treasury/pull/13

- فایل `frontend/shell/public/version.txt` حاوی `$Format:%h %cs$` است و در `.gitattributes` با `export-subst` علامت خورده. هم `git archive` و هم دانلود ZIP از GitHub، ‏commit و تاریخ را جای آن می‌گذارند.
- layout شل (`frontend/shell/app/layouts/default.vue`) فایل `/version.txt` را با `cache: 'no-store'` می‌گیرد. اگر جانگهدار جایگزین نشده باشد، مثلاً در اجرای dev یا یک `git clone` ساده، نشان نسخه را پنهان می‌کند. یک style در `shell.css` اضافه شده است.
- `deploy/windows/treasury.ps1` پیش از ساخت، نشان نسخه و پوشه را چاپ می‌کند.
- **Gateway (‏`deploy/gateway.conf`، ‏۲۷ آبان / <span dir="ltr">18 Nov</span>):**
    - مشکل: nginx نام میزبان‌های upstream را فقط یک بار هنگام شروع resolve می‌کرد. `docker compose up -d` پس از ساخت (کاری که اسکریپت ویندوز می‌کند) containerهای تغییرکرده را با نشانی IP تازه از نو می‌سازد ولی gateway را در حال اجرا می‌گذارد، پس gateway ترافیک را همچنان به نشانی‌های قدیمی می‌فرستاد.
    - در Docker در ۲۷ آبان دیده شد: `/` و `/version.txt` فایل `index.html` ریموت receipts را برمی‌گرداندند.
    - اصلاح: نام‌ها اکنون در متغیر قرار دارند و هنگام هر درخواست از راه DNS داخلی Docker resolve می‌شوند (`resolver 127.0.0.11 valid=10s ipv6=off`). پیشوند `/remotes/<x>/` با `rewrite … break` برداشته می‌شود، چون `proxy_pass` با متغیر، URI را بدون تغییر می‌فرستد.
    - بررسی شد: `nginx -t` موفق است. همه مسیرها 200 برمی‌گردانند (APIها، سه فایل `remoteEntry.js`، ‏`/version.txt`). از نو ساختن `shell`، ‏`remote-receipts` و `remote-basic-info` بدون دست‌زدن به gateway همچنان صفحه‌های درست را می‌داد.
- مستندات طبق خواسته CLAUDE.md به‌روز شد:
    - `README.md` / `README.en.md`: یادداشت «در انتظار بازبینی» زیر «آخرین تغییرها» و ردیف «استقرار» در «وضعیت فعلی»؛
    - `docs/test-server-windows.md` و `docs/reviewer-run-guide.fa.md`: نسخه کجا دیده می‌شود؛
    - `docs/plan.md`.
- pull request پیشین این شاخه (شماره ۸) ادغام شده بود؛ `main` در شاخه ادغام شد (`5df88c1`) نه rebase، پس تاریخچه بازنویسی نشده است.
- **بررسی شد:**
    - imageهای مربوط از یک ZIP ساخته‌شده با `git archive` ساخته و مجموعه در Docker راه‌اندازی شد؛ مرورگر نشان نسخه را در نوار بالا نشان داد.
    - `dotnet build Treasury.slnx` و `pnpm build` هر دو موفق‌اند.
    - آزمونی اضافه نشد: نشان نسخه فقط نمایشی است و تغییر gateway پیکربندی است، بدون قاعده کسب‌وکار. `deploy/smoke-test.sh` مسیرها را پوشش می‌دهد.
    - اسکریپت ویندوز اینجا اجرا نشده؛ PowerShell در این محیط کار در دسترس نیست.

<a id="pr-14"></a>

### pull request شماره ۱۴

**اقدام حقوقی برای چک‌های برگشتی، قاعده N3 («ثبت صیادی» پیش از بانک)، گواهی عدم پرداخت، فهرست دریافت‌ها، مشخصات شرکت**

باز از ۲۷ آبان ۱۴۰۵، CI: **`compose` ناموفق** · شاخه `claude/funny-wozniak-u12rbh` · https://github.com/BehinTaghavi/treasury/pull/14

- **کار یک نشست دیگر Claude (شاخه `claude/funny-wozniak-u12rbh`).** این pull request به درخواست مالک باز شد؛ پیام‌های commit هر تغییر را با جزئیات شرح می‌دهند. شاخه بر پایه `main` امروز است.
- **چرخه عمر:** وضعیت‌ها و مرحله‌های تازه `SayadRegistered`، ‏`InLegal`، ‏`ReturnFromLegal`؛ واگذاری به بانک از `SayadRegistered` شروع می‌شود (قاعده N3). تاریخچه قدیمی همان‌طور که ثبت شده مهاجرت می‌کند: `LEGACY_TRANSITIONS` در `tools/migration`.
- **حساب‌های الگوی تازه:** ‏`ProtestedChequesInLegal` و `GuaranteeChequesInLegal`.
- **شِما:** ‏033–037 (وضعیت چک، اسکن‌های اقدام حقوقی، سند حسابداری اقدام حقوقی، انواع واگذاری، پروفایل شرکت).
- **هم‌پوشانی با pull requestهای باز دیگر، که تصمیمش با بازبین است:**
    - **شماره ۱۱ («ثبت صیادی شد»)** همان قاعده صیاد را به شکل یک تیک روی چک می‌سازد (مالک، ۲۴ آبان / <span dir="ltr">15 Nov</span>). این pull request آن را یک مرحله و وضعیت می‌کند (قاعده N3، مالک، ۲۷ آبان / <span dir="ltr">18 Nov</span>). فقط یکی باید ادغام شود؛ این یکی از قاعده بعدی مالک پیروی می‌کند.
    - **شماره ۱۲ («اطلاعات شرکت»)** و صفحه «مشخصات شرکت» در اینجا هر دو نام و نشانی شرکت را در `reference-svc` نگه می‌دارند. پیش از ادغام هر دو، باید یک صفحه شوند.
    - **شماره ۱۰ (سفته)** همان کد چرخه عمر، رسید و سند حسابداری را تغییر می‌دهد؛ ادغام محلی ۲۶ فایل دارای تعارض داد که چندتای آن‌ها تعارض واقعی طراحی است. برای نمونه، آنجا سفته از `InCashDesk` واگذار می‌شود ولی اینجا فقط از `SayadRegistered`، و هر دو یک پارامتر اختیاری `ForStep` را برای دو منظور متفاوت اضافه کرده‌اند. یا شماره ۱۰ باید با این pull request سازگار شود، یا برعکس.
- **آزمون‌ها (محلی، روی `8ab8085` ادغام‌شده با شماره ۱۳ (`0c1705e`)، ‏PostgreSQL 17):**
    - ‎.NET در مجموع ۵۵۲، ‏۵۵۱ موفق، ۱ ردشده
    - آزمون‌های شِما ۴۲ موفق
    - آزمون‌های مهاجرت ۲۴ موفق
    - ui-kit ‏۱۶ موفق
    - `pnpm build` موفق
- **Docker:** مجموعه از یک ZIP ساخته‌شده با `git archive` ساخته و روی پایگاه داده بسته آزمایشی پیشین راه‌اندازی شد؛ اسکریپت‌های 036 و 037 بدون خطا اعمال شدند. تب‌های تازه و «مشخصات شرکت» بدون خطای صفحه باز می‌شوند.

</div>
