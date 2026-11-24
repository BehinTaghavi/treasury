<div dir="rtl">

# آشنایی ساده با مباحث بازبینی سیستم خزانه‌داری

> این سند هر موضوعی را که در `docs/reviewer-workflow.fa.md` (فایل PDF «گردش کار بازبین») آمده، **به زبان ساده** توضیح می‌دهد: چیست، چه کاری می‌کند، و بازبین **در شروع کار حداقل چه چیزی** از آن باید بداند تا بتواند کد خزانه‌داری و اسناد حسابداری را بازبینی کند. برای هر موضوع دو نوع منبع آمده است: **شروع سریع** (چند ساعت) و **عمیق‌تر** (کتاب یا دوره).
> ترتیب موضوع‌ها همان ترتیب مراحل گردش کار است. شماره مرحله در پرانتز آمده است.
> منابع بیشتر انگلیسی‌اند و نشانی‌ها به صفحه اصلی سایت رسمی اشاره می‌کنند. در آن سایت با همان عنوان جست‌وجو کنید. پیش از خرید کتاب، آخرین ویرایش آن را بگیرید.
> وضعیت مخزن: ۲ آذر ۱۴۰۵ (23 Nov 2026). مسیر یادگیری فنی‌تر و تمرین‌ها در فصل ۱۴ `docs/reviewer-guide.fa.md` است؛ عمق لازم هر تخصص در `docs/reviewer-skills.fa.md`.

## ترتیب پیشنهادی یادگیری در دو هفته اول

| روزها | موضوع‌ها (شماره‌های این سند) | چرا اول |
|---|---|---|
| ۱–۲ | ۱ تا ۵ (لینوکس، Git، GitHub، Docker، CI) | بدون این‌ها سیستم را نمی‌شود بالا آورد و Pull Request را نمی‌شود دید |
| ۳–۵ | ۲۳ تا ۲۶ و ۴۲ تا ۴۶ (حسابداری، اسناد خزانه‌داری، چک، پرداخت بانکی، تنخواه، کنترل داخلی، تاریخ شمسی) | زبان مشترک با مالک محصول؛ بدون آن قاعده اشتباه دیده نمی‌شود |
| ۶–۸ | ۶ تا ۱۵ (C#، معماری، DDD، تراکنش، آزمون) | قلب کد |
| ۹–۱۰ | ۱۶ تا ۲۲ (PostgreSQL و API)، ۳۵ (امنیت) و ۳۸–۳۹ (روش بازبینی) | پیش از اولین تأیید |
| بعد | بقیه، هر وقت در Pull Request به آن رسیدید | |

---

# بخش الف — ابزار کار روزانه (مرحله ۰ تا ۲)

## ۱. لینوکس و خط فرمان (مرحله ۰، ۱۳)

**به زبان ساده:** سیستم‌عاملی که سرورهای مشتری، CI و Docker روی آن کار می‌کنند. بازبین با خط فرمان (ترمینال) فرمان می‌نویسد، به‌جای کلیک.

**حداقل برای شروع:**
- جابه‌جایی بین پوشه‌ها، دیدن و جست‌وجوی فایل‌ها (`cd`، `ls`، `cat`، `less`، `grep`، `find`).
- اجرای اسکریپت (`bash script.sh`)، متغیر محیطی (`NAME=value command`)، دیدن لاگ و فرایندها (`ps`، `top`، `journalctl`).
- مجوز فایل‌ها (`chmod`) و اینکه چرا گذرواژه نباید در فایل باشد.

**منابع:**
- شروع سریع: **Linux Journey** (linuxjourney.com) — درس‌های کوتاه و رایگان.
- عمیق‌تر: کتاب رایگان **The Linux Command Line** از William Shotts (linuxcommand.org).

## ۲. Git (مرحله ۰، ۱)

**به زبان ساده:** دفتر ثبت همه تغییرات کد. هر تغییر یک «commit» است، هر خط کاری یک «شاخه» (branch). می‌شود دید چه کسی، چه وقت، چه چیزی را عوض کرده و هر نسخه قدیمی را دوباره گرفت.

**حداقل برای شروع:**
- `clone`، `fetch`، `switch`، `log`، `diff`، `status`.
- فرق شاخه `main` (نسخه تأییدشده) با شاخه‌های کاری `claude/...`.
- دیدن تفاوت یک شاخه با `main`: ‏`git diff origin/main...<branch>`.

**منابع:**
- شروع سریع: فصل‌های ۱ تا ۳ کتاب رایگان **Pro Git** (git-scm.com/book).
- عمیق‌تر: بقیه همان کتاب؛ و تمرین تعاملی **Learn Git Branching** (learngitbranching.js.org).

## ۳. GitHub و Pull Request (مرحله ۱، ۱۵)

**به زبان ساده:** سایتی که مخزن کد در آن است. «Pull Request» درخواست اضافه کردن یک شاخه به `main` است. بازبین آن را می‌خواند، روی خط‌ها نظر می‌نویسد و در پایان «تأیید» یا «درخواست اصلاح» می‌دهد. `main` محافظت‌شده است: بدون تأیید و CI سبز چیزی وارد نمی‌شود.

**حداقل برای شروع:**
- زبانه‌های «Conversation»، «Files changed»، «Checks».
- نظر روی یک خط، «Suggested change»، و سه نتیجه بازبینی: Approve، Request changes، Comment.

**منابع:**
- شروع سریع: **GitHub Docs** — «About pull requests» و «Reviewing changes in pull requests» (docs.github.com).
- عمیق‌تر: دوره‌های تعاملی رایگان **GitHub Skills** (skills.github.com)، به‌ویژه «Review pull requests».

## ۴. Docker و Docker Compose (مرحله ۰، ۱۱، ۱۳)

**به زبان ساده:** Docker هر بخش برنامه (هر سرویس، پایگاه داده، Keycloak، دروازه) را در یک «جعبه» آماده (container) اجرا می‌کند که روی هر سروری یکسان کار می‌کند. Docker Compose همه جعبه‌ها را با یک فرمان با هم بالا می‌آورد. کل سیستم خزانه‌داری روی سرور مشتری همین‌طور اجرا می‌شود.

**حداقل برای شروع:**
- `docker compose up -d --build`، `ps`، `logs`، `stop`، `down`.
- فرق image و container، و volume (جایی که داده پایگاه داده می‌ماند).
- خواندن `deploy/docker-compose.yml`: کدام سرویس به کدام وابسته است و کدام درگاه (port) باز است.

**منابع:**
- شروع سریع: **Docker Docs — Get started** (docs.docker.com).
- عمیق‌تر: کتاب **Docker Deep Dive** از Nigel Poulton.

## ۵. CI و GitHub Actions (مرحله ۲)

**به زبان ساده:** CI (یکپارچه‌سازی پیوسته) ماشینی است که با هر Pull Request خودکار کد را می‌سازد و همه آزمون‌ها را اجرا می‌کند. در این پروژه چهار «job» دارد: ‏.NET، پایگاه داده و مهاجرت، فرانت‌اند، و کل سیستم در Docker. سبز یعنی همه آزمون‌ها گذشته‌اند؛ قرمز یعنی چیزی شکسته است.

**حداقل برای شروع:**
- پیدا کردن job قرمز و خواندن خطای آن در زبانه «Checks».
- اینکه CI سبز **شرط لازم** است، نه کافی: CI نمی‌داند قاعده درست فهمیده شده یا نه.

**منابع:**
- شروع سریع: **GitHub Docs — Understanding GitHub Actions** (docs.github.com/actions).
- عمیق‌تر: کتاب **Continuous Delivery** از Jez Humble و David Farley (مفهوم، نه ابزار).

---

# بخش ب — کد بک‌اند (مرحله ۴ تا ۸)

## ۶. C# و ‎.NET 10 (مرحله ۵ تا ۸)

> **کجای کد:**
>
> - `global.json`، `Directory.Build.props` — نسخه ‎.NET و تنظیمات مشترک؛ «هر هشدار خطاست»
> - `Treasury.slnx`، `Treasury.Offline.slnf` — فهرست پروژه‌ها، عادی و Offline
> - `src/BuildingBlocks/Treasury.BuildingBlocks.Domain/Primitives.cs` — پایه مشترک همه سرویس‌ها
> - `src/Services/Cashflow/Treasury.Cashflow.Domain/Common/Money.cs`، `Instruments/InstrumentTypes.cs` — نمونه خوب C# امروزی: `record`، pattern matching

**به زبان ساده:** C# زبان برنامه‌نویسی بک‌اند است و ‎.NET بستری که آن را اجرا می‌کند (نسخه ۱۰، از مایکروسافت، متن‌باز، روی لینوکس هم کار می‌کند). همه قواعد کسب‌وکار خزانه‌داری به C# نوشته شده‌اند.

**حداقل برای شروع:**
- کلاس، `record`، متد، `if`/`switch`، فهرست‌ها و LINQ (`Where`، `Sum`).
- `decimal` برای پول (هرگز `double`)، و nullable (`string?`).
- خواندن خطای build و اینکه در این پروژه **هر هشدار خطاست**.

**منابع:**
- شروع سریع: **Microsoft Learn — C# fundamentals** و «What's new in C#» (learn.microsoft.com/dotnet/csharp).
- عمیق‌تر: کتاب‌های سری **Modern Cross-Platform Development Fundamentals** از Mark J. Price (آخرین ویرایش)؛ یا **C# in a Nutshell** از Joseph Albahari برای مرجع.

## ۷. ASP.NET Core Minimal APIs و تزریق وابستگی (مرحله ۸)

> **کجای کد:**
>
> - `src/Services/<Service>/Treasury.<Service>.Api/Program.cs` — تزریق وابستگی؛ انتخاب پایگاه داده واقعی یا در حافظه با متغیر `usePostgres`
> - `src/Services/<Service>/Treasury.<Service>.Api/Endpoints.cs` — endpointها؛ در Reference چند فایل `*Endpoints.cs`

**به زبان ساده:** ASP.NET Core بخشی از ‎.NET است که درخواست‌های مرورگر را می‌گیرد و جواب می‌دهد. هر «endpoint» یک نشانی است، مثل «ثبت دریافت» یا «فهرست بانک‌ها». «تزریق وابستگی» یعنی هر بخش آنچه لازم دارد (مثلاً پایگاه داده واقعی یا نسخه آزمایشی در حافظه) را از بیرون می‌گیرد، نه اینکه خودش بسازد.

**حداقل برای شروع:**
- endpoint باید **نازک** باشد: ورودی بگیرد، فرمان را صدا بزند، جواب بدهد. قاعده در آن نباشد.
- پیدا کردن جایی در `Program.cs` که پایگاه داده واقعی یا در حافظه انتخاب می‌شود.

**منابع:**
- شروع سریع: **Microsoft Learn — Minimal APIs overview** و «Dependency injection in ASP.NET Core».
- عمیق‌تر: کتاب **ASP.NET Core in Action** از Andrew Lock (ویرایش سوم یا بعد).

## ۸. async/await (مرحله ۵، ۶)

> **کجای کد:**
>
> - `src/Services/Cashflow/Treasury.Cashflow.Infrastructure.Postgres/PostgresAdapters.cs` — `PostgresUnitOfWork` با `AsyncLocal`
> - `src/BuildingBlocks/Treasury.BuildingBlocks.Application/CommandRunner.cs` — اجرای هر فرمان
> - `src/Services/<Service>/Treasury.<Service>.Application/**/*UseCases.cs` — کاربردها؛ همه `async`

**به زبان ساده:** وقتی برنامه منتظر پایگاه داده یا سرویس دیگر است، به‌جای ایستادن کار دیگری می‌کند. `await` یعنی «اینجا منتظر جواب بمان».

**حداقل برای شروع:**
- هر جا `Task` برمی‌گردد باید `await` شود؛ `.Result` یا `.Wait()` پرچم قرمز است.
- `CancellationToken` یعنی امکان لغو کار طولانی.

**منابع:**
- شروع سریع: **Microsoft Learn — Asynchronous programming with async and await**.
- عمیق‌تر: کتاب **Concurrency in C# Cookbook** از Stephen Cleary.

## ۹. معماری تمیز (Clean Architecture) (مرحله ۵ تا ۸)

> **کجای کد:**
>
> - `src/Services/<Service>/Treasury.<Service>.Domain|Application|Infrastructure|Infrastructure.Postgres|Api` — چهار لایه هر سرویس
> - `src/BuildingBlocks/` — دو پروژه مشترک Domain و Application
> - `tests/Treasury.ArchitectureTests/LayerTests.cs` — آزمون خودکار جهت وابستگی لایه‌ها
> - `docs/architecture.md`، `docs/adr/0001-modular-services-few-processes.md` — توضیح معماری

**به زبان ساده:** کد هر سرویس چهار لایه دارد، مثل پیاز: در مرکز **Domain** (قواعد کسب‌وکار)، دورش **Application** (اجرای هر کار، مثل «ثبت دریافت»)، بعد **Infrastructure** (پایگاه داده، فایل Excel) و بیرونی‌ترین **Api** (درگاه وب). لایه درونی هیچ‌وقت لایه بیرونی را نمی‌شناسد. نتیجه: قاعده کسب‌وکار به پایگاه داده یا صفحه وابسته نیست و جدا آزمون می‌شود. آزمون‌های معماری پروژه این را خودکار می‌سنجند.

**حداقل برای شروع:**
- هر فایل تغییرکرده در کدام لایه است، و آیا چیزی که باید در Domain باشد به لایه دیگر رفته است.

**منابع:**
- شروع سریع: کتاب الکترونیکی رایگان مایکروسافت **Architect Modern Web Applications with ASP.NET Core and Azure**، فصل «Common web application architectures».
- عمیق‌تر: کتاب **Clean Architecture** از Robert C. Martin؛ مقاله **Hexagonal Architecture** از Alistair Cockburn.

## ۱۰. DDD (طراحی دامنه‌محور) (مرحله ۵)

> **کجای کد:**
>
> - `src/BuildingBlocks/Treasury.BuildingBlocks.Domain/Primitives.cs` — `AggregateRoot`، `IDomainEvent`، `DomainException`
> - `src/Services/Cashflow/Treasury.Cashflow.Domain/Instruments/Instrument.cs` — تجمیع چک و سفته
> - `src/Services/Cashflow/Treasury.Cashflow.Domain/Receipts/Receipt.cs`، `ChequeBooks/ChequeBook.cs`، `Vouchers/Vouchers.cs` — تجمیع‌های دریافت، دسته چک، سند تأییدشده
> - `src/Services/Cashflow/Treasury.Cashflow.Infrastructure/Snapshots.cs` — تبدیل تجمیع به ردیف ذخیره و برعکس

**به زبان ساده:** روشی برای نوشتن نرم‌افزار که **زبان کد همان زبان کسب‌وکار** باشد. «چک»، «دریافت»، «سند» در کد هم همین نام‌ها را دارند. چند مفهوم اصلی:
- **Aggregate (تجمیع):** یک واحد کسب‌وکار که همیشه با هم ذخیره می‌شود و از قواعدش محافظت می‌کند. مثلاً یک چک با همه گام‌هایش.
- **Value Object:** مقداری که هویت ندارد، مثل مبلغ یا تاریخ.
- **رویداد دامنه:** خبر اینکه اتفاقی افتاد («چک وصول شد»).

**حداقل برای شروع:**
- قاعده فقط از راه متدهای خود تجمیع عوض می‌شود (مثلاً `Deposit`)، نه با تغییر مستقیم مقدارها از بیرون.

**منابع:**
- شروع سریع: کتاب کوتاه **Domain-Driven Design Distilled** از Vaughn Vernon.
- عمیق‌تر: **Learning Domain-Driven Design** از Vlad Khononov؛ و کتاب رایگان مایکروسافت **.NET Microservices: Architecture for Containerized .NET Applications**، فصل‌های DDD.

## ۱۱. ماشین وضعیت (چرخه عمر چک و سفته) (مرحله ۵، ۱۲)

> **کجای کد:**
>
> - `src/Services/Cashflow/Treasury.Cashflow.Domain/Instruments/InstrumentLifecycle.cs` — تنها جدول تغییر وضعیت‌های مجاز
> - `tools/migration/treasury_migration/legacy_mapping.py` — کپی Python همان جدول
> - `tests/Treasury.Cashflow.Domain.Tests/InstrumentLifecycleTests.cs`، `ChequeStepTests.cs`، `NoteStepTests.cs` — آزمون‌ها
> - `tools/migration/tests/test_mapping.py` — آزمون یکسانی دو جدول
> - `docs/domain/instrument-lifecycle.fa.md` — توضیح برای مالک محصول

**به زبان ساده:** فهرست وضعیت‌های یک چک («نزد صندوق»، «ثبت صیادی»، «واگذار شده به بانک»، «وصول شده»، «برگشتی»، «اقدام حقوقی»…) و **اینکه از هر وضعیت به کدام وضعیت‌ها می‌شود رفت**. در این پروژه این جدول فقط یک جا تعریف شده (`InstrumentLifecycle.cs`) و تغییر آن تصمیم مالک محصول است.

**حداقل برای شروع:**
- کشیدن نمودار وضعیت‌های چک دریافتی روی کاغذ از روی `docs/domain/instrument-lifecycle.fa.md`.
- هر Pull Request که این جدول را عوض کند باید تصمیم ثبت‌شده مالک را همراه داشته باشد.

**منابع:**
- شروع سریع: صفحه **State** در refactoring.guru (الگوی وضعیت، با تصویر).
- عمیق‌تر: همین سند دامنه در مخزن، همراه آزمون‌های `Treasury.Cashflow.Domain.Tests`.

## ۱۲. تراکنش (مرحله ۶، ۷)

> **کجای کد:**
>
> - `src/BuildingBlocks/Treasury.BuildingBlocks.Application/Ports.cs` — `IUnitOfWork`
> - `src/BuildingBlocks/Treasury.BuildingBlocks.Application/CommandRunner.cs` — هر فرمان در یک تراکنش
> - `src/Services/Cashflow/Treasury.Cashflow.Infrastructure.Postgres/PostgresAdapters.cs` — `PostgresUnitOfWork`
> - `src/Services/Cashflow/Treasury.Cashflow.Infrastructure/InMemory/InMemoryStore.cs` — نسخه در حافظه برای آزمون
> - `docs/adr/0002-one-transaction-per-command.md` — تصمیم معماری

**به زبان ساده:** چند تغییر که یا **همه با هم** ثبت می‌شوند یا **هیچ‌کدام**. اگر وسط ثبت دریافت برق برود، نیمی از دریافت ثبت نمی‌شود. قاعده پروژه: «یک فرمان = یک تراکنش = یک تجمیع که یک بار ذخیره می‌شود».

**حداقل برای شروع:**
- پرچم قرمز: دو بار ذخیره در یک کار، یا «اگر خطا شد قبلی را پاک کن» (برنامه قدیمی همین مشکل را داشت).

**منابع:**
- شروع سریع: مستندات PostgreSQL، فصل **Transactions** در بخش Tutorial (postgresql.org/docs).
- عمیق‌تر: کتاب **Designing Data-Intensive Applications** از Martin Kleppmann، فصل ۷ (Transactions).

## ۱۳. Idempotency (کلید یکتایی) (مرحله ۶، ۸)

> **کجای کد:**
>
> - `src/BuildingBlocks/Treasury.BuildingBlocks.Application/CommandRunner.cs`، `Ports.cs` — `IIdempotencyStore`
> - `src/Services/Cashflow/Treasury.Cashflow.Infrastructure.Postgres/PostgresAdapters.cs` — `PostgresIdempotencyStore`
> - `db/postgres/002_cashflow.sql` — جدول `cashflow.idempotency`
> - `src/Services/<Service>/Treasury.<Service>.Api/Endpoints.cs` — `Key(http)`: هدر `Idempotency-Key`
> - `tests/Treasury.Cashflow.Application.Tests/InstrumentUseCaseTests.cs` — آزمون `The_same_idempotency_key_does_not_create_a_second_instrument`

**به زبان ساده:** اگر کاربر دکمه «ثبت» را دو بار بزند یا اینترنت قطع و وصل شود و درخواست دوباره برسد، **دو دریافت ثبت نشود**. هر درخواست نوشتن یک کلید یکتا دارد؛ درخواست دوم با همان کلید فقط جواب اول را برمی‌گرداند.

**حداقل برای شروع:**
- هر کار نوشتن کلید یکتایی می‌گیرد، و آزمونی هست که نشان دهد ارسال دوباره، سند دوم نمی‌سازد.

**منابع:**
- شروع سریع: مقاله **Designing robust and predictable APIs with idempotency** در وبلاگ Stripe (stripe.com/blog).
- عمیق‌تر: الگوی **Idempotent Consumer** در microservices.io.

## ۱۴. Outbox و کنترل همزمانی (مرحله ۶)

> **کجای کد:**
>
> - `src/BuildingBlocks/Treasury.BuildingBlocks.Domain/Primitives.cs` — `AggregateRoot`: نسخه ردیف و رویدادهای در انتظار
> - `src/Services/Cashflow/Treasury.Cashflow.Infrastructure.Postgres/PostgresAdapters.cs` — `PostgresOutbox`؛ `AND version = @expected`؛ `FOR UPDATE` روی دسته چک
> - `src/BuildingBlocks/Treasury.BuildingBlocks.Application/Ports.cs` — `ConcurrencyConflictException`
> - `db/postgres/002_cashflow.sql`، `001_reference.sql` — جدول‌های `outbox`
> - `tests/Treasury.Cashflow.Application.Tests/InstrumentUseCaseTests.cs`، `tests/Treasury.Cashflow.Infrastructure.Postgres.Tests/PostgresAdapterTests.cs` — آزمون `Parallel_issues_from_one_book_never_get_the_same_number`

**به زبان ساده:**
- **Outbox:** وقتی کاری انجام شد و باید به سرویس دیگری خبر داده شود (مثلاً در آینده: «این سند را به دفتر کل بفرست»)، خبر در **همان تراکنش** در یک جدول نوشته می‌شود و بعد ارسال می‌شود. پس خبر هیچ‌وقت گم نمی‌شود و دو بار هم اثر نمی‌کند.
- **کنترل همزمانی خوش‌بینانه:** اگر دو کاربر همزمان یک چک را تغییر دهند، دومی خطای «تعارض» می‌گیرد، به‌جای اینکه کار اولی را بی‌صدا از بین ببرد.

**حداقل برای شروع:**
- در هر Pull Request بپرسید: «اگر دو نفر همزمان همین کار را بکنند چه می‌شود؟»

**منابع:**
- شروع سریع: الگوی **Transactional Outbox** در microservices.io.
- عمیق‌تر: مستندات PostgreSQL، فصل **Concurrency Control**؛ و همان فصل ۷ کتاب Kleppmann.

## ۱۵. آزمون خودکار و xUnit (مرحله ۴)

> **کجای کد:**
>
> - `tests/Treasury.<Service>.Domain.Tests` — آزمون قواعد
> - `tests/Treasury.<Service>.Application.Tests` — آزمون کاربردها با آداپتور در حافظه
> - `tests/Treasury.<Service>.Infrastructure.Postgres.Tests`، `tests/Support/PostgresDatabase.cs` — آزمون با PostgreSQL واقعی
> - `tests/Treasury.ArchitectureTests`، `tests/Treasury.Reference.Infrastructure.Excel.Tests` — معماری و خواندن Excel
> - `tests/Treasury.TestShim` — جایگزین xUnit در حالت Offline

**به زبان ساده:** برنامه کوچکی که برنامه اصلی را امتحان می‌کند: «اگر چکی که هنوز ثبت صیادی نشده به بانک واگذار شود، باید با خطای فلان رد شود». این پروژه بیش از ۶۰۰ آزمون ‎.NET دارد و قاعده‌اش «هر قاعده یک آزمون» است. **آزمون توصیفی** (characterization) آزمونی است که رفتار برنامه قدیم را ثبت می‌کند تا برنامه جدید همان را انجام دهد.

**حداقل برای شروع:**
- خواندن نام آزمون‌ها مثل فهرست قواعد.
- آزمون خوب هم حالت مجاز را می‌سنجد هم حالت رد را، و در حالت رد **کد خطا** و **تغییرنکردن وضعیت** را چک می‌کند.
- اجرای `dotnet test` و خواندن نتیجه.

**منابع:**
- شروع سریع: **xUnit.net — Getting started** (xunit.net)؛ **Microsoft Learn — Unit testing best practices for .NET**.
- عمیق‌تر: **Unit Testing: Principles, Practices, and Patterns** از Vladimir Khorikov؛ برای آزمون توصیفی و کار با برنامه قدیمی: **Working Effectively with Legacy Code** از Michael Feathers.

---

# بخش ج — پایگاه داده و API (مرحله ۷، ۸)

## ۱۶. SQL و PostgreSQL (مرحله ۷)

> **کجای کد:**
>
> - `db/postgres/000_roles.sql` تا `042_cashflow_issued_cheque_sayad_tick.sql` — ۴۳ اسکریپت ساخت پایگاه داده
> - `db/postgres/apply.sh` — اجرای اسکریپت‌ها به ترتیب
> - `src/Services/<Service>/Treasury.<Service>.Infrastructure.Postgres/*.cs` — SQL دست‌نویس هر سرویس

**به زبان ساده:** PostgreSQL پایگاه داده‌ای است که همه اطلاعات (چک‌ها، دریافت‌ها، اسناد، اطلاعات پایه) در آن نگه‌داری می‌شود. SQL زبان پرسیدن از آن و نوشتن در آن است.

**حداقل برای شروع:**
- `SELECT`، `WHERE`، `JOIN`، `INSERT`، `UPDATE`، `CREATE TABLE`.
- **پارامتر به‌جای چسباندن متن:** مقدارهای کاربر هرگز مستقیم داخل متن SQL گذاشته نمی‌شوند (مرحله ۱۴، SQL injection).

**منابع:**
- شروع سریع: بخش **Tutorial** مستندات رسمی PostgreSQL 17 (postgresql.org/docs/17)؛ یا سایت **PostgreSQL Tutorial** (postgresqltutorial.com).
- عمیق‌تر: کتاب **The Art of PostgreSQL** از Dimitri Fontaine.

## ۱۷. نقش‌ها و دسترسی‌ها، schema (مرحله ۷)

> **کجای کد:**
>
> - `db/postgres/000_roles.sql` — نقش هر سرویس
> - `GRANT`های انتهای هر اسکریپت `db/postgres/0*.sql` — دسترسی هر نقش
> - `db/postgres/tests/test_schema.py` — آزمون نقش‌ها و دسترسی‌ها
> - سرویس `db-init` در `deploy/docker-compose.yml` — گذرواژه نقش‌ها از `.env`

**به زبان ساده:** هر سرویس (دریافت‌ها، اطلاعات پایه، حساب‌ها، درخواست وجه) در پایگاه داده **بخش جدای خودش (schema)** و **کاربر جدای خودش (نقش)** را دارد و فقط به جدول‌های خودش دسترسی دارد. پس یک خطا یا نفوذ در یک سرویس به داده سرویس دیگر نمی‌رسد.

**حداقل برای شروع:**
- جدول تازه باید `GRANT` همان نقش سرویس را داشته باشد؛ ارجاع یا پرسش بین دو schema ممنوع است.

**منابع:**
- شروع سریع: مستندات PostgreSQL — **Database Roles** و **Privileges**.
- عمیق‌تر: همان مستندات — **Schemas** و **Row Security Policies**.

## ۱۸. پارتیشن‌بندی بر اساس سال مالی (مرحله ۷)

> **کجای کد:**
>
> - `db/postgres/003_fiscal_year_partitions.sql` — `ensure_fiscal_year_partitions`، `open_fiscal_year`، `fiscal_year_of`
> - `db/postgres/002_cashflow.sql`، `025_cashflow_receipts.sql`، `026_requests.sql`، `030_cashflow_vouchers.sql` — جدول‌های `PARTITION BY LIST`
> - `docs/adr/0003-fiscal-year-partitioning.md` — تصمیم معماری

**به زبان ساده:** جدول‌های بزرگ (مثل چک‌ها) برای هر سال مالی یک «قفسه» جدا دارند. گزارش یک سال فقط قفسه همان سال را می‌خواند و سریع است، و سال بسته‌شده را می‌شود قفل کرد.

**حداقل برای شروع:**
- جدول تراکنشی تازه باید بر اساس سال مالی پارتیشن شود (ADR 0003).

**منابع:**
- شروع سریع: مستندات PostgreSQL — **Table Partitioning**.
- عمیق‌تر: همان صفحه، بخش «Partition Pruning»، و آزمون‌های `db/postgres/tests`.

## ۱۹. قیدها، اسکریپت‌های قابل اجرای دوباره، `NOT VALID` (مرحله ۷)

> **کجای کد:**
>
> - `db/postgres/031_cashflow_cheque_steps.sql`، `018_accounts_petty_cash_and_cash_desk_details.sql` — قیدهای `NOT VALID`
> - `db/postgres/apply.sh` — اجرای دوباره همه اسکریپت‌ها در هر شروع
> - `db/postgres/tests/test_schema.py` — آزمون اجرای دوباره روی داده موجود

**به زبان ساده:** «قید» (constraint) قاعده‌ای در خود پایگاه داده است که داده نادرست را رد می‌کند، مثلاً تکراری‌نبودن شماره برگ چک؛ خط دفاع دوم پشت قاعده Domain. اسکریپت‌های پایگاه داده در هر بار روشن شدن سیستم دوباره اجرا می‌شوند، پس باید بی‌خطر قابل تکرار باشند. `NOT VALID` یعنی «قید را برای داده‌های تازه اجرا کن، ولی داده‌های قدیمی را فعلاً رد نکن».

**حداقل برای شروع:**
- اسکریپت تازه شماره بعدی را دارد و دو بار اجرای آن خطا نمی‌دهد.
- درس ۲۱ نوامبر: یک اسکریپت با هر شروع فهرست قدیمی وضعیت‌ها را برمی‌گرداند و سیستم با داده موجود بالا نمی‌آمد.

**منابع:**
- شروع سریع: مستندات PostgreSQL — **Constraints** و دستور **ALTER TABLE** (بخش `NOT VALID`).
- عمیق‌تر: مستندات PostgreSQL — **Triggers** و **PL/pgSQL**.

## ۲۰. Npgsql (مرحله ۷)

> **کجای کد:**
>
> - `src/Services/Cashflow/Treasury.Cashflow.Infrastructure.Postgres/PostgresAdapters.cs`، `PostgresReceipts.cs`، `PostgresVouchers.cs` — نمونه‌های اصلی
> - `src/Services/Reference|Accounts|Requests/Treasury.*.Infrastructure.Postgres/` — آداپتورهای سه سرویس دیگر
> - `tests/Support/PostgresDatabase.cs` — پایگاه داده آزمون

**به زبان ساده:** کتابخانه‌ای که برنامه C# با آن به PostgreSQL وصل می‌شود. این پروژه SQL را دستی می‌نویسد (بدون ORM)، پس هر خط SQL در کد C# دیده می‌شود.

**حداقل برای شروع:**
- تشخیص اینکه مقدارها با پارامتر (`@name`) فرستاده شده‌اند، نه با چسباندن رشته.

**منابع:**
- شروع سریع: **Npgsql — Basic usage** (npgsql.org/doc).
- عمیق‌تر: **Microsoft Learn — ADO.NET overview**.

## ۲۱. SQL Server و ابزار مهاجرت با Python (مرحله ۱۲)

> **کجای کد:**
>
> - `tools/migration/treasury_migration/source.py` — خواندن SQL Server قدیمی
> - `tools/migration/treasury_migration/legacy_mapping.py`، `jalali.py` — نگاشت ردیف قدیمی و تاریخ ۸ نویسه‌ای
> - `tools/migration/treasury_migration/target.py`، `__main__.py` — نوشتن در PostgreSQL و گزارش تطبیق
> - `tools/migration/tests/`، `tools/migration/README.md` — آزمون‌ها و راهنما
> - `db/postgres/tests/test_schema.py` — نمونه دیگر pytest

**به زبان ساده:** برنامه قدیمی اطلاعاتش را در SQL Server (پایگاه‌هایی مثل `Cashnajoo..`) نگه می‌داشت. ابزار مهاجرت، نوشته‌شده با Python، آن‌ها را می‌خواند، به مدل جدید تبدیل می‌کند و **گزارش تطبیق** می‌دهد: تعداد و جمع ریالی، مورد انتظار در برابر منتقل‌شده. pytest ابزار آزمون Python است.

**حداقل برای شروع:**
- هیچ ردیفی بی‌صدا دور ریخته نمی‌شود؛ ردیف ردشده با دلیل در گزارش می‌آید.
- خواندن یک آزمون pytest و اجرای `pytest`.

**منابع:**
- شروع سریع: **The Python Tutorial** (docs.python.org)؛ **pytest — Get started** (docs.pytest.org)؛ **Microsoft Learn — Tutorial: Writing Transact-SQL statements**.
- عمیق‌تر: کتاب **Python Testing with pytest** از Brian Okken؛ مقاله **Strangler Fig Application** در martinfowler.com (روش جایگزینی تدریجی سیستم قدیم).

## ۲۲. HTTP، کدهای وضعیت و Problem Details (مرحله ۸)

> **کجای کد:**
>
> - `src/Services/<Service>/Treasury.<Service>.Api/Endpoints.cs` — `ToHttp`: کد خطا ← وضعیت HTTP
> - `frontend/packages/ui-kit/src/api.ts` — خواندن پاسخ و خطا در صفحه
> - `frontend/packages/ui-kit/src/i18n.ts` — متن فارسی و انگلیسی هر کد خطا

**به زبان ساده:** مرورگر و سرور با HTTP حرف می‌زنند. هر جواب یک عدد دارد: 200 یعنی موفق، 404 یعنی پیدا نشد، 409 یعنی تعارض (کس دیگری همزمان تغییر داده)، 422 یعنی قاعده کسب‌وکار اجازه نمی‌دهد. «Problem Details» قالب استاندارد پیام خطاست؛ در این پروژه خطا همیشه یک **کد ثابت** است (مثل `Instrument.InvalidTransition`) و صفحه، متن فارسی آن را نشان می‌دهد.

**حداقل برای شروع:**
- معنای 200، 201، 400، 404، 409، 422 و 503.
- کد خطای تازه ترجمه فارسی و انگلیسی دارد.

**منابع:**
- شروع سریع: **MDN — HTTP response status codes** (developer.mozilla.org).
- عمیق‌تر: **RFC 9457** (Problem Details for HTTP APIs)؛ کتاب **HTTP: The Definitive Guide** (O'Reilly) برای مرجع.

---

# بخش د — اسناد حسابداری و دانش کسب‌وکار (مرحله ۹)

## ۲۳. حسابداری دوطرفه (مرحله ۹)

**به زبان ساده:** هر رویداد مالی دست‌کم دو طرف دارد: یک حساب **بدهکار** و یک حساب **بستانکار**، و جمع بدهکار همیشه با جمع بستانکار برابر است (سند **تراز** است). مثال: چکی از مشتری دریافت می‌شود ← «اسناد نزد صندوق» بدهکار، و حساب مشتری (از گروه بابت) بستانکار.

**حداقل برای شروع:**
- ساختار حساب‌ها: **گروه ← کل ← معین ← (معین در معین)**، و **تفصیل** که شخص، بانک یا مرکز هزینه را مشخص می‌کند.
- نوشتن سند هر سطر جدول `docs/domain/posting-rules.fa.md` با یک مبلغ فرضی، و تشخیص سندی که **جهتش برعکس** است.
- سند تأییدشده عوض نمی‌شود؛ اصلاح فقط با سند جدید (سند برگشت) است.

**منابع:**
- شروع سریع: سایت رایگان **AccountingCoach** (accountingcoach.com) — بخش‌های «Debits and Credits» و «Accounting Basics»؛ کتاب بسیار کوتاه **Accounting Made Simple** از Mike Piper.
- عمیق‌تر: دوره **Introduction to Financial Accounting** دانشگاه Wharton در Coursera؛ به فارسی: هر کتاب درسی دانشگاهی **«اصول حسابداری ۱»** و **«استانداردهای حسابداری ایران»** منتشرشده توسط **سازمان حسابرسی**.

## ۲۴. اسناد خزانه‌داری در این پروژه (مرحله ۹)

**به زبان ساده:** برای هر دریافت و هر گام چک (واگذاری، وصول، برگشت، اقدام حقوقی…) سیستم یک **پیش‌نویس سند** می‌سازد. حساب‌های ثابت از صفحه «شابلون اسناد خزانه داری» می‌آیند و بقیه از اطلاعات پایه (حساب بانک، گروه بابت، صندوق). خزانه‌دار با «تایید سند» آن را قطعی می‌کند. در فاز F این اسناد به دفتر کل سیستم حسابداری فرستاده می‌شوند.

**حداقل برای شروع:**
- خواندن جدول شابلون مالک (`posting-rules.fa.md`) و یافتن سطر هر عملیات.
- چهار چیز که در هر سند تازه چک می‌شود: حساب بدهکار، حساب بستانکار، تفصیل هر طرف، تراز بودن.
- سند ناقص (بدون حساب) تأیید نمی‌شود؛ کد حساب ثابت در برنامه نوشته نمی‌شود.

**منابع:**
- شروع سریع: `docs/domain/posting-rules.fa.md` و ردیف‌های مربوط در `docs/decisions.md`.
- عمیق‌تر: آزمون‌های `tests/Treasury.Cashflow.Domain.Tests/VoucherTests.cs` (نام هر آزمون یک قاعده سند است)؛ راهنماهای کاربر برنامه قدیم در `legacy/manuals/`.

## ۲۵. عملیات چک و خزانه‌داری در ایران (مرحله ۹، ۱۱)

**به زبان ساده:** چرخه یک چک دریافتی: دریافت و نگه‌داری در صندوق ← **ثبت در سامانه صیاد** بانک مرکزی ← واگذاری به بانک برای وصول ← وصول یا **برگشت** ← در صورت برگشت، **گواهی عدم پرداخت** و **اقدام حقوقی**. همچنین چک روز و مدت‌دار، چک و سفته **انتظامی (تضمینی)**، خرج چک (واگذاری به دیگری)، و تنخواه.

**حداقل برای شروع:**
- معنای همه وضعیت‌ها در `docs/domain/instrument-lifecycle.fa.md` و `received-cheque-steps.fa.md`.
- قاعده‌های اصلی مالک: N2 (وصول نه قبل از سررسید)، N3 (ثبت صیادی پیش از واگذاری به بانک).

**منابع:**
- شروع سریع: همان دو سند دامنه، و راهنمای کاربر برنامه قدیم در `legacy/manuals/`.
- عمیق‌تر: متن **قانون صدور چک** (با اصلاحیه ۱۳۹۷) و صفحه‌های **سامانه صیاد** در سایت بانک مرکزی (cbi.ir)؛ بخش اسناد تجاری (برات، سفته، چک) در **قانون تجارت**.

## ۲۶. تقویم شمسی و متن فارسی در نرم‌افزار (مرحله ۵)

> **کجای کد:**
>
> - `src/BuildingBlocks/Treasury.BuildingBlocks.Domain/FiscalDate.cs` — تاریخ واقعی و `ParseJalali`
> - `src/BuildingBlocks/Treasury.BuildingBlocks.Domain/PersianText.cs` — `PersianText.Key`: یکسان‌سازی ی/ي، ک/ك، نیم‌فاصله
> - `frontend/packages/ui-kit/src/format.ts`، `words.ts` — نمایش تاریخ و عدد، مبلغ به حروف
> - `tools/migration/treasury_migration/jalali.py` — تاریخ‌های قدیمی
> - `tests/Treasury.Cashflow.Domain.Tests/ValueObjectTests.cs`، `tools/migration/tests/test_jalali.py` — آزمون‌ها

**به زبان ساده:** در دامنه و پایگاه داده تاریخ واقعی نگه‌داری می‌شود و «۱۴۰۵/۰۸/۱۸» فقط در صفحه نمایش داده یا از آن گرفته می‌شود. متن فارسی چند گونه نوشتاری دارد: «ی» و «ي»، «ک» و «ك»، نیم‌فاصله. سیستم پیش از مقایسه آن‌ها را یکسان می‌کند تا «بانک ملی» و «بانك ملي» یکی شمرده شوند.

**حداقل برای شروع:**
- تاریخ در کد `FiscalDate` است، نه متن؛ مقایسه نام‌ها با `PersianText.Key` است.

**منابع:**
- شروع سریع: **Microsoft Learn — PersianCalendar class**.
- عمیق‌تر: سند W3C **Arabic & Persian Layout Requirements** (w3.org/TR/alreq).

---

# بخش هـ — فرانت‌اند و دیدن در مرورگر (مرحله ۱۰، ۱۱)

## ۲۷. TypeScript (مرحله ۱۰)

> **کجای کد:**
>
> - `frontend/packages/ui-kit/src/*.ts` — کد مشترک
> - `frontend/remotes/<remote>/src/*.ts` — مثلاً `receipts/src/lookups.ts`، `basic-info/src/types.ts`
> - `tsconfig.json` هر بخش — تنظیمات TypeScript

**به زبان ساده:** JavaScript (زبان مرورگر) به‌اضافه «نوع». برنامه‌نویس می‌گوید هر مقدار عدد است یا متن یا فلان ساختار، و بسیاری از خطاها پیش از اجرا گرفته می‌شوند.

**حداقل برای شروع:** خواندن `type`، `interface`، و نوع‌های اختیاری (`?`).

**منابع:**
- شروع سریع: **TypeScript Handbook — The Basics** (typescriptlang.org/docs).
- عمیق‌تر: کتاب **Effective TypeScript** از Dan Vanderkam.

## ۲۸. Vue 3 (مرحله ۱۰)

> **کجای کد:**
>
> - `frontend/remotes/receipts/src/` — `ReceiptForm.vue`، `ChequeOperations.vue`، `LegalAction.vue`، `VoucherTable.vue`، …
> - `frontend/remotes/basic-info/src/` — اطلاعات پایه؛ `CatalogueList.vue` ساده‌ترین نمونه
> - `frontend/remotes/requests/src/` — درخواست وجه
> - `frontend/packages/ui-kit/src/AmountInput.vue`، `SearchSelect.vue` — کامپوننت‌های مشترک

**به زبان ساده:** کتابخانه ساخت صفحه‌های وب. هر صفحه از «کامپوننت»‌ها ساخته می‌شود، مثل فرم دریافت چک یا فهرست دریافت‌ها، و هر کامپوننت HTML، منطق نمایش و ظاهر خودش را دارد.

**حداقل برای شروع:**
- خواندن یک فایل `.vue` (`<template>`، `<script setup>`، `ref`، `computed`).
- صفحه درباره قاعده تصمیم نمی‌گیرد؛ تصمیم با سرور است.

**منابع:**
- شروع سریع: آموزش تعاملی رسمی **Vue Tutorial** (vuejs.org/tutorial).
- عمیق‌تر: **Vue Guide** کامل (vuejs.org/guide)؛ کتاب **Vue.js 3 Design Patterns and Best Practices** (Packt) یا دوره‌های Vue School.

## ۲۹. Nuxt 4، Vite، Module Federation، pnpm (مرحله ۱۰)

> **کجای کد:**
>
> - `frontend/shell/nuxt.config.ts` — پوسته و نشانی سه micro-frontend
> - `frontend/shell/app/` — `layouts/default.vue`، `pages/*.vue`، `plugins/federation.client.ts`، `components/RemoteArea.vue`
> - `frontend/remotes/<remote>/vite.config.ts` — Vite و Module Federation هر بخش
> - `frontend/package.json`، `pnpm-workspace.yaml`، `pnpm-lock.yaml` — pnpm
> - `deploy/shell-config.json`، `frontend/Dockerfile` — تنظیم و ساخت برای Docker

**به زبان ساده:**
- **Nuxt:** «پوسته» برنامه: منوی اصلی، نوار بالا با نام و لوگوی شرکت، و جای بخش‌ها.
- **Module Federation (micro-frontend):** هر بخش (اطلاعات پایه، دریافت‌ها، درخواست وجه) برنامه جدایی است که پوسته هنگام اجرا بار می‌کند. پس هر بخش را می‌شود جدا ساخت و به‌روز کرد.
- **Vite:** ابزار ساخت (build) این بخش‌ها.
- **pnpm:** مدیر بسته‌های JavaScript؛ فایل `pnpm-lock.yaml` نسخه دقیق هر بسته را قفل می‌کند.

**حداقل برای شروع:**
- بخش تازه باید در پوسته، دروازه، Docker و CI ثبت شود. بسته تازه متن‌باز باشد و lockfile همراهش به‌روز شود.

**منابع:**
- شروع سریع: **Nuxt Docs — Introduction** (nuxt.com/docs)؛ **Vite Guide** (vite.dev/guide)؛ **pnpm — Motivation و Workspaces** (pnpm.io).
- عمیق‌تر: مستندات **Module Federation** (module-federation.io)؛ مقاله **Micro Frontends** از Cam Jackson در martinfowler.com.

## ۳۰. Vitest (مرحله ۴، ۱۰)

> **کجای کد:**
>
> - `frontend/packages/ui-kit/src/codes.test.ts` — ترجمه همه کدهای خطا
> - `frontend/packages/ui-kit/src/chequeStatus.test.ts`، `words.test.ts`، `ui-kit.test.ts` — آزمون‌های دیگر

**به زبان ساده:** ابزار آزمون خودکار فرانت‌اند. در این پروژه مهم‌ترین آزمونش این است که **هر کد خطا ترجمه فارسی و انگلیسی دارد**.

**حداقل برای شروع:** اجرای `pnpm test` و خواندن نتیجه.

**منابع:**
- شروع سریع: **Vitest — Getting Started** (vitest.dev).
- عمیق‌تر: **Vue — Testing** در vuejs.org/guide.

## ۳۱. صفحه فارسی راست‌به‌چپ، تلفن و تبلت (مرحله ۱۰، ۱۱)

> **کجای کد:**
>
> - `frontend/shell/nuxt.config.ts`، `frontend/remotes/<remote>/index.html` — `dir="rtl"`
> - `frontend/shell/app/assets/shell.css` — ظاهر پوسته
> - `frontend/remotes/receipts/src/receipts.css`، `requests/src/requests.css`، `basic-info/src/organization/organization.css` — `@media` برای اندازه‌های کوچک
> - `docs/adr/0006-phones-and-tablets.md` — تصمیم معماری

**به زبان ساده:** همه صفحه‌ها راست‌به‌چپ‌اند و طبق تصمیم مالک (ADR 0006) باید روی تلفن (عرض ۳۶۰ پیکسل) و تبلت (۷۶۸ پیکسل) هم قابل استفاده باشند. در مرورگر کروم با DevTools می‌شود هر اندازه صفحه را شبیه‌سازی کرد.

**حداقل برای شروع:**
- باز کردن DevTools (کلید F12)، حالت **Device Toolbar**، و امتحان فرم در ۳۶۰ و ۷۶۸ پیکسل: بدون اسکرول افقی، با دکمه‌هایی که با انگشت زده می‌شوند.

**منابع:**
- شروع سریع: **Chrome DevTools — Simulate mobile devices with Device Mode** (developer.chrome.com/docs/devtools)؛ **RTL Styling 101** از Ahmad Shadeed (rtlstyling.com).
- عمیق‌تر: **MDN — Responsive design** و **CSS logical properties** (developer.mozilla.org).

## ۳۲. smoke test (مرحله ۱۱)

> **کجای کد:**
>
> - `deploy/smoke-test.sh` — خود آزمون
> - job `compose` در `.github/workflows/ci.yml` — اجرا در CI، با توقف و شروع دوباره
> - `docs/reviewer-run-guide.fa.md` — راهنمای اجرا

**به زبان ساده:** آزمون سریع «آیا سیستم سالم بالا آمده؟». اسکریپت `deploy/smoke-test.sh` همه بخش‌ها را از دروازه صدا می‌زند و یک چک آزمایشی را ثبت، ثبت صیادی و واگذار می‌کند. فقط روی سرور آزمایشی اجرا می‌شود، چون داده می‌نویسد.

**حداقل برای شروع:** اجرای آن طبق `docs/reviewer-run-guide.fa.md` و خواندن پیام `OK` یا `FAIL`.

**منابع:** همان راهنمای اجرا در مخزن؛ برای مفهوم: **Smoke testing** در Wikipedia.

---

# بخش و — استقرار (مرحله ۱۳)

## ۳۳. Nginx (دروازه) (مرحله ۱۳)

> **کجای کد:**
>
> - `deploy/gateway.conf` — مسیرهای دروازه
> - `deploy/docker-compose.yml`، `deploy/.env.example` — سرویس gateway و `GATEWAY_ADDRESS`
> - `frontend/nginx.conf` — Nginx داخل کانتینرهای فرانت‌اند

**به زبان ساده:** «در ورودی» سیستم. همه درخواست‌ها به آن می‌رسند و آن هر درخواست را به بخش درست می‌فرستد (صفحه‌ها، API دریافت‌ها، Keycloak…). فعلاً فقط روی خود سرور (`127.0.0.1`) باز است، چون ورود کاربر هنوز فعال نیست.

**حداقل برای شروع:** هر تغییری که دروازه را به شبکه باز کند، پیش از فعال شدن Keycloak پرچم قرمز امنیتی است.

**منابع:**
- شروع سریع: **nginx — Beginner's Guide** (nginx.org/en/docs).
- عمیق‌تر: **NGINX Reverse Proxy** در docs.nginx.com.

## ۳۴. ویندوز و PowerShell (مرحله ۱۳)

> **کجای کد:**
>
> - `deploy/windows/treasury.ps1` — اسکریپت اصلی
> - `deploy/windows/Start Treasury.cmd`، `Stop Treasury.cmd`، `Reset test data.cmd` — دکمه‌های دوبار کلیک
> - `docs/test-server-windows.md` — راهنمای مالک

**به زبان ساده:** سرور آزمایشی مالک روی ویندوز است و با دوبار کلیک روی «Start Treasury» اجرا می‌شود. پشت آن یک اسکریپت PowerShell است (`deploy/windows/treasury.ps1`).

**حداقل برای شروع:** اگر Pull Request چیزی در `deploy/` عوض کرد، این اسکریپت‌ها و `docs/test-server-windows.md` هم باید به‌روز شده باشند.

**منابع:**
- شروع سریع: **Microsoft Learn — PowerShell 101**.
- عمیق‌تر: کتاب **Learn PowerShell in a Month of Lunches** (Manning).

---

# بخش ز — امنیت و ورود کاربر (مرحله ۱۴، ۱۷)

## ۳۵. امنیت برنامه وب (مرحله ۱۴)

> **کجای کد:**
>
> - `src/Services/<Service>/Treasury.<Service>.Infrastructure.Postgres/` — SQL پارامتری
> - `.gitignore`، `deploy/.env.example` — رازها و فایل‌هایی که هرگز commit نمی‌شوند (`.env`، `.mdb`، Excel)
> - `db/postgres/000_roles.sql` — کمترین دسترسی
> - `src/Services/Cashflow/Treasury.Cashflow.Domain/Instruments/InstrumentAttachment.cs` — نوع و اندازه فایل بارگذاری‌شده (`MaxBytes`)
> - `src/Services/<Service>/Treasury.<Service>.Api/Endpoints.cs` — `RequireAuthorization`

**به زبان ساده:** فهرست خطاهای امنیتی رایج که یک سیستم مالی نباید داشته باشد. مهم‌ترین‌ها برای این پروژه:
- **SQL injection:** کاربر با تایپ متن خاص در یک فیلد، دستور پایگاه داده اجرا کند. راه‌حل: پارامتر.
- **رازها در کد:** گذرواژه و کلید هرگز در مخزن نیست؛ فقط در فایل `.env` روی سرور.
- **کمترین دسترسی:** هر بخش فقط به همان چیزی که لازم دارد دسترسی دارد.
- **داده مشتری:** هیچ داده واقعی ایران ناژو، پشتیبان یا فایل `.mdb` در مخزن نیست.

**حداقل برای شروع:** چک‌لیست مرحله ۱۴ گردش کار را در هر Pull Request اجرا کنید.

**منابع:**
- شروع سریع: **OWASP Top 10** (owasp.org، آخرین ویرایش)؛ از **OWASP Cheat Sheet Series** (cheatsheetseries.owasp.org) دو برگه «SQL Injection Prevention» و «Secrets Management».
- عمیق‌تر: **OWASP Code Review Guide**؛ کتاب **Alice and Bob Learn Application Security** از Tanya Janca.

## ۳۶. ورود کاربر: Keycloak، OAuth 2.0، OpenID Connect، JWT (مرحله ۸، ۱۷)

> **کجای کد:**
>
> - `src/Services/<Service>/Treasury.<Service>.Api/Program.cs` — `AddJwtBearer`، `Keycloak:Authority`؛ بیرون از Development بدون آن بالا نمی‌آید
> - `src/Services/Cashflow/Treasury.Cashflow.Api/Endpoints.cs` — `HttpCurrentUser`
> - `src/Services/Cashflow/Treasury.Cashflow.Application/Abstractions/Ports.cs` — `ICurrentUser`
> - سرویس `keycloak` در `deploy/docker-compose.yml` — سرور هویت
> - `docs/adr/0005-multi-tenancy-companies-and-groups.md` — ورود همراه با چند شرکت

**به زبان ساده:** Keycloak سرور «ورود و مجوز» است. کاربر یک بار وارد می‌شود و یک «بلیت» دیجیتال امضاشده (JWT) می‌گیرد که می‌گوید کیست و چه حقوقی دارد. هر سرویس بلیت را بررسی می‌کند. OAuth 2.0 و OpenID Connect استانداردهای همین کارند. کار بعدی پروژه (فاز B) ورود کاربر **همراه با چند شرکت** است: هر کاربر در هر شرکت حقوق جدا دارد (ADR 0005).

**حداقل برای شروع:**
- endpoint تازه بدون مجوز (`RequireAuthorization`) باز نمانده باشد.
- هیچ فرمانی داده دو شرکت را با هم تغییر ندهد.

**منابع:**
- شروع سریع: **OAuth 2.0 Simplified** از Aaron Parecki (oauth.com)؛ **Keycloak — Getting started with Docker** (keycloak.org).
- عمیق‌تر: کتاب **OAuth 2 in Action** (Manning)؛ **Microsoft Learn — Configure JWT bearer authentication in ASP.NET Core**.

## ۳۷. مجوز بسته‌ها (لایسنس) (مرحله ۱۴)

> **کجای کد:**
>
> - `src/**/*.csproj`، `tests/**/*.csproj` — بسته‌های NuGet (`PackageReference`)
> - `frontend/**/package.json` — بسته‌های npm
> - `tools/migration/requirements.txt` — بسته‌های Python
> - `CLAUDE.md` — فهرست بسته‌های ممنوع

**به زبان ساده:** هر کتابخانه‌ای که به پروژه اضافه می‌شود مجوز استفاده دارد. این پروژه فقط کتابخانه متن‌باز می‌پذیرد. چند کتابخانه معروف در نسخه‌های جدیدشان پولی شده‌اند و ممنوع‌اند: MediatR ≥13، AutoMapper ≥15، MassTransit ≥9، FluentAssertions ≥8.

**حداقل برای شروع:** با هر بسته تازه در `.csproj` یا `package.json`، مجوز آن را در صفحه‌اش (nuget.org یا npmjs.com) ببینید.

**منابع:** **choosealicense.com** (توضیح ساده مجوزها)؛ فهرست مجوزهای متن‌باز در **opensource.org/licenses**.

---

# بخش ح — روش کار بازبینی (مرحله ۳، ۱۵)

## ۳۸. بازبینی کد، به‌ویژه کدی که هوش مصنوعی نوشته (مرحله ۱۵)

> **کجای کد:**
>
> - `.github/pull_request_template.md` — الگوی توضیح Pull Request
> - `.github/workflows/ci.yml` — بررسی‌های خودکار
> - `CLAUDE.md` — «Rules for every change»
> - `docs/reviewer-workflow.fa.md`، `docs/reviewer-guide.fa.md` (فصل ۱۳) — گردش کار و چک‌لیست

**به زبان ساده:** بازبینی یعنی یک انسان پیش از ورود کد به نسخه اصلی آن را بخواند و بپرسد. در این پروژه کد را هوش مصنوعی می‌نویسد. کد هوش مصنوعی معمولاً **مرتب و قانع‌کننده** است، حتی وقتی قاعده را اشتباه فهمیده باشد. پس معیار تأیید «مرتب به نظر می‌رسد» نیست؛ معیار این است: **قاعده منبع دارد، آزمون دارد، و آزمون واقعاً همان قاعده را می‌سنجد.**

**حداقل برای شروع:**
- ترتیب خواندن: توضیح Pull Request ← مستندات تصمیم ← آزمون‌ها ← کد.
- نظر روشن و محترمانه، با جدا کردن «مانع» از «پیشنهاد».

**منابع:**
- شروع سریع: **Google Engineering Practices — Code Review** (google.github.io/eng-practices/review).
- عمیق‌تر: کتاب **Looks Good to Me: Constructive Code Reviews** از Adrienne Braganza (Manning)؛ فصل ۱۳ `docs/reviewer-guide.fa.md`.

## ۳۹. مستندات تصمیم و ADR (مرحله ۳)

> **کجای کد:**
>
> - `docs/adr/0001` تا `0006` — تصمیم‌های معماری
> - `docs/decisions.md` — تصمیم‌های مالک محصول
> - `docs/domain/open-questions.md`، `.fa.md` — پرسش‌های باز
> - `docs/plan.md` — برنامه و کارهای انجام‌شده

**به زبان ساده:** هر تصمیم کسب‌وکاری که مالک محصول می‌گیرد با تاریخ و دلیل در `docs/decisions.md` ثبت می‌شود. پرسش‌هایی که هنوز جواب ندارند در `docs/domain/open-questions.md` هستند. **ADR** (سند تصمیم معماری) همین کار را برای تصمیم‌های فنی بزرگ می‌کند، مثل «یک پایگاه داده برای هر شرکت».

**حداقل برای شروع:**
- قاعده‌ای که در کد هست ولی در این دو فایل ردی ندارد، یعنی حدس هوش مصنوعی؛ باید پرسیده شود.
- خواندن شش ADR در `docs/adr/`.

**منابع:**
- شروع سریع: مقاله **Documenting Architecture Decisions** از Michael Nygard؛ سایت **adr.github.io**.
- عمیق‌تر: کتاب **Fundamentals of Software Architecture** از Mark Richards و Neal Ford (فصل تصمیم‌های معماری).

## ۴۰. خواندن برنامه قدیمی (MS Access و ASP.NET MVC) (مرحله ۳، ۴)

> **کجای کد:**
>
> - `legacy/README.md` — راهنمای پوشه
> - `legacy/access/` — `vba`، `queries`، `lookup_tables` برنامه `Lcash.mdb`
> - `legacy/mvc/`، `legacy/db/` — نسخه ASP.NET MVC و رویه‌های ذخیره‌شده بازیابی‌شده
> - `legacy/manuals/` — راهنماهای کاربر
> - `legacy/print-service/` — سرویس چاپ قدیمی

**به زبان ساده:** برنامه قدیمی (`Lcash.mdb`، بیش از ۲۰ سال در کار) با Access و زبان VBA نوشته شده و یک نسخه نیمه‌کاره هم با ASP.NET MVC وجود دارد. هر دو در پوشه `legacy/` هستند، **فقط برای خواندن**: برای اینکه بفهمیم برنامه قدیم چه می‌کرد، نه اینکه از آن کپی کنیم یا اجرایش کنیم.

**حداقل برای شروع:**
- پیدا کردن فرم یا ماژولی که یک قاعده به آن ارجاع داده است (`legacy/README.md` راهنماست)، و خواندن کلی منطق یک تابع VBA.

**منابع:**
- شروع سریع: **Microsoft Learn — Office VBA reference** (بخش Access) و «Getting started with VBA in Office».
- عمیق‌تر: کتاب **Working Effectively with Legacy Code** از Michael Feathers (روش، نه ابزار).

## ۴۱. نوشتن و گفت‌وگو (مرحله ۳، ۱۵)

> **کجای کد:**
>
> - `README.md`، `README.en.md` — صفحه اصلی فارسی و نسخه انگلیسی
> - `docs/domain/*.md` و `*.fa.md` — مستندات دوزبانه دامنه
> - `.github/pull_request_template.md` — دو بخش توضیح: برای مالک و برای بازبین
> - `frontend/packages/ui-kit/src/i18n.ts` — متن پیام‌ها برای کاربر

**به زبان ساده:** بازبین با دو مخاطب حرف می‌زند: با **مالک محصول** (غیربرنامه‌نویس) به زبان ساده و بدون اصطلاح فنی، یک فهرست کوتاه پرسش در هر نوبت؛ و با **نویسنده کد** روی همان خط کد. مستندات پروژه دوزبانه‌اند و توضیح فنی Pull Requestها انگلیسی است.

**حداقل برای شروع:**
- تبدیل یک یافته فنی به یک پرسش کسب‌وکار. مثال: «اگر چک برگشتی به صندوق برگردد، سندش بدهکار اسناد نزد صندوق است یا بستانکار؟»

**منابع:**
- شروع سریع: **Google Technical Writing One** (developers.google.com/tech-writing) — دوره کوتاه و رایگان.
- عمیق‌تر: **Google Technical Writing Two** از همان سایت.

---

# بخش ط — دانش کسب‌وکار تکمیلی (مرحله ۹، ۱۷)

## ۴۲. پرداخت و انتقال بانکی در ایران: شبا، پایا، ساتنا، کارت به کارت، کارتخوان (مرحله ۹)

**به زبان ساده:** پول به چند راه به حساب بانکی شرکت می‌رسد یا از آن می‌رود: فیش واریز، **پایا** (انتقال گروهی بین‌بانکی، چند نوبت در روز)، **ساتنا** (انتقال فوری بین‌بانکی برای مبلغ‌های بزرگ‌تر)، **کارت به کارت** و **کارتخوان (POS)**. **شبا** شماره ۲۶ نویسه‌ای استاندارد هر حساب بانکی (با IR) است. در این برنامه شش نوع واریز مستقیم هست و برای پایا و ساتنا شبای پرداخت‌کننده گرفته می‌شود (تصمیم مالک، ۱۸ نوامبر).

**حداقل برای شروع:**
- فرق این روش‌ها، و اینکه هر کدام در فرم دریافت چه اطلاعاتی می‌خواهد.
- سند همه آن‌ها بدهکار معین حساب بانکی با تفصیل همان حساب است.

**منابع:**
- شروع سریع: صفحه‌های **پایا**، **ساتنا** و **شبا** در بخش «نظام‌های پرداخت» سایت بانک مرکزی (cbi.ir)؛ ردیف‌های ۱۴ و ۱۸ نوامبر در `docs/decisions.md`.
- عمیق‌تر: کتاب **Payments Systems in the U.S.** از Carol Coye Benson برای مفاهیم کلی نظام‌های پرداخت (ساختار پایا و ساتنا شبیه ACH و RTGS است).

## ۴۳. صندوق و تنخواه‌گردان (مرحله ۹)

**به زبان ساده:** **صندوق** پول نقد و اسنادی است که در شرکت نگه‌داری می‌شود و یک صندوقدار دارد. **تنخواه‌گردان** مبلغ ثابتی است که به یک نفر (تنخواه‌دار) داده می‌شود تا هزینه‌های کوچک را بپردازد. سپس با ارائه مدارک، مبلغ خرج‌شده به او برگردانده می‌شود (**ترمیم تنخواه**). در این برنامه هر تنخواه سقف، حد تسویه و گروه‌های بابت مجاز دارد. موجودی اول دوره صندوق‌ها و تنخواه‌ها هم ثبت می‌شود.

**حداقل برای شروع:**
- سند پرداخت از تنخواه و سند ترمیم آن در جدول `posting-rules.fa.md`.
- پرداخت از تنخواه نباید از سقف یا گروه‌های بابت مجاز بیرون برود.

**منابع:**
- شروع سریع: درس **Petty Cash** در سایت AccountingCoach (accountingcoach.com)؛ بخش تنخواه در راهنماهای کاربر برنامه قدیم (`legacy/manuals/`).
- عمیق‌تر: فصل «وجه نقد» در کتاب درسی **«اصول حسابداری ۱»**.

## ۴۴. درخواست وجه، گردش تأیید و کنترل داخلی (مرحله ۹، ۱۷)

**به زبان ساده:** در سیستم مالی هیچ‌کس نباید یک پرداخت را به‌تنهایی از اول تا آخر انجام دهد: یکی درخواست می‌کند، دیگری تأیید می‌کند و سومی پرداخت می‌کند (**تفکیک وظایف**). به این قواعد **کنترل داخلی** می‌گویند. در این برنامه «درخواست وجه» همین گردش است. «دارندگان امضای مجاز» هم مشخص می‌کنند هر سند چند امضا و از چه کسانی می‌خواهد. با فعال شدن ورود کاربر، حق تأیید و حق «تایید سند» به نقش‌ها گره می‌خورد.

**حداقل برای شروع:**
- بپرسید: آیا یک کاربر می‌تواند هم درخواست کند و هم تأیید؟ آیا سند تأییدشده بدون ردپا عوض می‌شود؟
- هر کار مالی ثبت می‌کند چه کسی و چه وقت آن را انجام داده است.

**منابع:**
- شروع سریع: درس **Internal Control** در AccountingCoach؛ خلاصه اجرایی **Internal Control — Integrated Framework** در سایت COSO (coso.org).
- عمیق‌تر: بخش کنترل‌های داخلی در **استانداردهای حسابرسی** منتشرشده توسط سازمان حسابرسی؛ همان چارچوب کامل COSO.

## ۴۵. مغایرت‌گیری بانکی و تطبیق با سیستم قدیم (مرحله ۱۲، ۱۷)

**به زبان ساده:** **صورت مغایرت بانکی** گردش حساب بانکی در دفاتر شرکت را با صورت‌حساب بانک مقایسه می‌کند و تفاوت‌ها را توضیح می‌دهد (مثلاً چکی که واگذار شده ولی بانک هنوز وصول نکرده). شرط پذیرش فاز F و پایلوت هم نوعی تطبیق است: در یک ماه **اجرای موازی**، سندها و مانده‌های برنامه جدید باید **تا ریال** با برنامه قدیم یکی باشند. گزارش تطبیق ابزار مهاجرت هم همین کار را برای داده منتقل‌شده می‌کند.

**حداقل برای شروع:**
- خواندن یک گزارش تطبیق: تعداد و جمع ریالی مورد انتظار در برابر ثبت‌شده، و توضیح هر اختلاف.

**منابع:**
- شروع سریع: درس **Bank Reconciliation** در AccountingCoach.
- عمیق‌تر: فصل «صورت مغایرت بانکی» در **«اصول حسابداری ۱»**؛ `tools/migration/README.md` در مخزن.

## ۴۶. ارز خارجی و معادل ریالی (مرحله ۹)

**به زبان ساده:** چک یا حساب بانکی ممکن است ارزی باشد، مثلاً یورو. برای ثبت در دفاتر، مبلغ با نرخ روز به **معادل ریالی** تبدیل می‌شود و سند همیشه ریالی و تراز است. موجودی اول دوره حساب‌های ارزی هم معادل ریالی دارد.

**حداقل برای شروع:**
- هر سطر سند معادل ریالی دارد و جمع ریالی بدهکار و بستانکار برابر است.
- نرخ تبدیل و تاریخ آن معلوم و ثبت‌شده است.

**منابع:**
- شروع سریع: درس **Foreign Currency Transactions** در AccountingCoach یا هر منبع آموزشی مقدماتی درباره IAS 21.
- عمیق‌تر: **استاندارد حسابداری شماره ۱۶ ایران (آثار تغییر در نرخ ارز)** منتشرشده توسط سازمان حسابرسی؛ استاندارد بین‌المللی **IAS 21** در سایت ifrs.org.

---

## جدول خلاصه: موضوع ← مرحله گردش کار ← عمق لازم

عمق‌ها از `docs/reviewer-skills.fa.md` آمده‌اند (بازبین اصلی / بازبین دوم).

| # | موضوع | مرحله | عمق (اصلی / دوم) |
|---|---|---|---|
| ۱ | لینوکس | ۰، ۱۳ | ۸ / ۶ |
| ۲–۳ | Git و GitHub | ۱، ۱۵ | ۸ / ۷ |
| ۴ | Docker | ۰، ۱۱، ۱۳ | ۷ / ۶ |
| ۵ | CI | ۲ | ۸ / ۷ |
| ۶ | C# و ‎.NET 10 | ۵–۸ | ۸ / ۶ |
| ۷ | ASP.NET Core | ۸ | ۷ / ۵ |
| ۸ | async/await | ۵، ۶ | ۷ / ۴ |
| ۹ | معماری تمیز | ۵–۸ | ۸ / ۵ |
| ۱۰ | DDD | ۵ | ۸ / ۵ |
| ۱۱ | ماشین وضعیت | ۵، ۱۲ | ۶ / ۵ |
| ۱۲–۱۴ | تراکنش، idempotency، outbox | ۶ | ۸ / ۵ |
| ۱۵ | آزمون | ۴ | ۸ / ۶ |
| ۱۶–۱۹ | PostgreSQL | ۷ | ۸ / ۵ |
| ۲۰ | Npgsql | ۷ | ۶ / ۳ |
| ۲۱ | SQL Server و Python | ۱۲ | ۵ / ۴ |
| ۲۲ | HTTP | ۸ | ۷ / ۵ |
| ۲۳–۲۴ | حسابداری و اسناد | ۹ | ۵ / ۸ |
| ۲۵ | چک و خزانه‌داری | ۹، ۱۱ | ۵ / ۷ |
| ۲۶ | تاریخ و متن فارسی | ۵ | ۵ / ۵ |
| ۲۷–۲۸ | TypeScript و Vue | ۱۰ | ۴ / ۷ |
| ۲۹ | Nuxt، Module Federation | ۱۰ | ۳ / ۶ |
| ۳۰ | Vitest | ۴، ۱۰ | ۳ / ۵ |
| ۳۱ | راست‌به‌چپ و تلفن | ۱۰، ۱۱ | ۴ / ۷ |
| ۳۳ | Nginx | ۱۳ | ۵ / ۴ |
| ۳۴ | PowerShell | ۱۳ | ۳ / ۴ |
| ۳۵ | امنیت وب | ۱۴ | ۸ / ۶ |
| ۳۶ | Keycloak و OAuth | ۸، ۱۷ | ۷ / ۴ |
| ۳۸ | بازبینی کد | ۱۵ | ۹ / ۸ |
| ۴۰ | برنامه قدیمی | ۳، ۴ | ۵ / ۴ |
| ۴۱ | نوشتن و گفت‌وگو | ۳، ۱۵ | ۷ / ۷ |
| ۴۲–۴۶ | پرداخت بانکی، تنخواه، کنترل داخلی، مغایرت‌گیری، ارز | ۹، ۱۲، ۱۷ | ۵ / ۷ |

</div>
