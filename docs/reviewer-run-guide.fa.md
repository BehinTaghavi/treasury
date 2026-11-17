<div dir="rtl">

# راهنمای بازبین: ساخت و اجرای سیستم خزانه‌داری با Docker

این راهنما برای بازبین (reviewer) پروژه است: کسی که Git و Docker دارد و می‌خواهد **خودش، از صفر و بدون هیچ ابزار هوش مصنوعی**، هر نسخه از سیستم (شاخه `main` یا یک pull request) را بسازد، اجرا کند و فرم‌ها را در مرورگر ببیند.

- فقط **Git** و **Docker** لازم است. نصب .NET، Node.js یا PostgreSQL لازم نیست؛ همه چیز داخل Docker ساخته می‌شود.
- این همان چیزی است که مالک محصول روی ویندوز با دوبار کلیک اجرا می‌کند (`docs/test-server-windows.md`) و همان چیزی که CI در job به نام `compose` روی هر pull request می‌سازد: فایل `deploy/docker-compose.yml`.
- آخرین بررسی: ۲۴ آبان ۱۴۰۵ (15 Nov 2026). همه دستورهای این راهنما از یک clone تازه روی لینوکس (Docker 29، Compose v5) اجرا و نتیجه‌ها دیده شد. مسیر دستی روی ویندوز و macOS جداگانه آزمایش نشده است؛ نکته‌های مخصوص ویندوز در بخش ۱ آمده. پس از ادغام pull request شماره ۸ (کارهای ۲۰ تا ۲۴ آبان) فهرست سرویس‌ها، منوها و نشانی‌ها از روی `deploy/docker-compose.yml`، `deploy/gateway.conf` و `deploy/smoke-test.sh` به‌روز شد؛ همین ساخت و smoke test را job به نام `compose` در CI روی آن pull request اجرا کرده و سبز بوده است.

> **فقط داده آزمایشی.** سیستم هنوز صفحه ورود (login) ندارد. داده واقعی شرکت ایران ناژو را در آن وارد نکنید و آن را روی شبکه باز نکنید (بخش ۷).

## خلاصه برای کسی که عجله دارد

```bash
git clone https://github.com/BehinTaghavi/treasury.git
cd treasury
cp deploy/.env.example deploy/.env
docker compose -f deploy/docker-compose.yml --env-file deploy/.env up -d --build
deploy/smoke-test.sh
```

سپس در مرورگر: **http://localhost:8080**

بقیه این راهنما همین پنج خط را توضیح می‌دهد و می‌گوید برای بازبینی یک pull request، دیدن لاگ‌ها، پاک کردن داده و رفع خطا چه باید کرد.

## ۱. پیش‌نیازها

| مورد | توضیح |
|---|---|
| Docker | Docker Desktop (ویندوز، macOS) یا Docker Engine (لینوکس)، همراه با **Compose نسخه ۲ یا جدیدتر**. بررسی: `docker compose version` (با فاصله، نه `docker-compose`). |
| Git | هر نسخه جدید. در ویندوز «Git for Windows» که **Git Bash** را هم نصب می‌کند. |
| حافظه | دست‌کم ۸ گیگابایت RAM (در Docker Desktop دست‌کم ۶ گیگابایت به Docker بدهید: *Settings → Resources*). |
| دیسک | حدود ۱۰ گیگابایت جای خالی (image‌ها حدود ۳ گیگابایت، حافظه نهان ساخت حدود ۳ گیگابایت). |
| پورت | پورت `8080` آزاد باشد (قابل تغییر، بخش ۴). |
| اینترنت | هنگام ساخت، Docker از این نشانی‌ها دانلود می‌کند: Docker Hub (`postgres`، `nginx`، `node`)، `mcr.microsoft.com` (‎.NET)، `quay.io` (Keycloak)، `registry.npmjs.org` و `api.nuget.org` (بسته‌ها). اگر شبکه شما به بعضی از این‌ها دسترسی ندارد، بخش ۱۰ را ببینید. |

**کدام خط فرمان؟** در لینوکس و macOS ترمینال معمولی. در ویندوز **Git Bash** را باز کنید؛ همه دستورهای این راهنما در آن کار می‌کنند. (در PowerShell هم دستورهای `git` و `docker` همین‌ها هستند، ولی `deploy/smoke-test.sh` به bash نیاز دارد.)

**نکته ویندوز، پایان خط‌ها:** برای اینکه فایل‌ها دقیقاً همان‌طور که در مخزن هستند (با پایان خط LF) روی دیسک بیایند، clone را با این گزینه بگیرید:

```bash
git clone -c core.autocrlf=false https://github.com/BehinTaghavi/treasury.git
```

## ۲. گرفتن کد

```bash
git clone https://github.com/BehinTaghavi/treasury.git
cd treasury
```

مخزن خصوصی است؛ Git نام کاربری و توکن (یا کلید SSH) حساب GitHub شما را می‌خواهد. همه دستورهای بعدی از **همین پوشه (ریشه مخزن)** اجرا می‌شوند.

## ۳. انتخاب نسخه‌ای که می‌خواهید ببینید

**آخرین نسخه تأییدشده (`main`):**

```bash
git switch main
git pull
```

**یک pull request (مثلاً شماره ۱۲؛ شماره در نشانی صفحه PR هست: `.../pull/12`):**

```bash
git fetch origin pull/12/head:pr-12
git switch pr-12
```

اگر بعداً commit تازه‌ای به همان PR اضافه شد:

```bash
git switch pr-12
git pull --ff-only origin pull/12/head
```

برای اینکه بدانید الان دقیقاً کدام نسخه روی دیسک است: `git log --oneline -1`

## ۴. فایل تنظیمات (`deploy/.env`)

رمزها و پورت در فایل `deploy/.env` هستند. این فایل در `.gitignore` است و **هرگز نباید commit شود**.

```bash
cp deploy/.env.example deploy/.env
```

سپس فایل را با یک ویرایشگر باز کنید:

- مقدارهای `change-me...` را با رمزهای دلخواه خودتان عوض کنید (فقط حروف و رقم انگلیسی، بدون فاصله و بدون `'` یا `$`).
- `GATEWAY_PORT=8080`: اگر پورت ۸۰۸۰ روی دستگاه شما اشغال است، عدد دیگری بگذارید (مثلاً `8081`)؛ نشانی مرورگر هم همان می‌شود.
- `GATEWAY_ADDRESS=127.0.0.1` را **تغییر ندهید**: یعنی فقط همین دستگاه می‌تواند سیستم را باز کند.

دو نکته مهم:

1. **`POSTGRES_PASSWORD` بعد از اولین اجرا ثابت می‌ماند.** پایگاه داده با همان رمز اولین اجرا ساخته می‌شود. اگر بعداً آن را در `.env` عوض کنید، سرویس `db-init` با خطای `password authentication failed for user "postgres"` می‌ایستد. راه‌حل: رمز قبلی را برگردانید یا داده آزمایشی را پاک کنید (بخش ۸، «پاک کردن همه داده»).
2. **با اضافه شدن سرویس جدید، متغیر جدید هم اضافه می‌شود.** اگر بعد از گرفتن نسخه تازه این خطا را دیدید: `required variable ... is missing a value: set in .env`، آن متغیر را از `deploy/.env.example` به `deploy/.env` خودتان اضافه کنید.

## ۵. ساخت و اجرا

```bash
docker compose -f deploy/docker-compose.yml --env-file deploy/.env up -d --build
```

- **بار اول** بسته به سرعت اینترنت ۵ تا ۲۰ دقیقه طول می‌کشد (دانلود image‌های پایه و بسته‌ها، سپس ساخت چهار سرویس ‎.NET و چهار بخش رابط کاربری). دفعه‌های بعد معمولاً زیر یک دقیقه.
- `--build` را همیشه بنویسید؛ اگر کد عوض نشده باشد Docker از حافظه نهان استفاده می‌کند و سریع است.

بررسی وضعیت:

```bash
docker compose -f deploy/docker-compose.yml --env-file deploy/.env ps -a
```

باید این دوازده سرویس را ببینید:

| سرویس | کار | وضعیت درست |
|---|---|---|
| `postgres` | پایگاه داده PostgreSQL 17 | `running (healthy)` |
| `db-init` | اسکریپت‌های `db/postgres` را اجرا می‌کند، رمز نقش‌ها را می‌گذارد، سال‌های مالی را باز می‌کند | **`exited (0)`**؛ این درست است، کارش را کرده و تمام شده |
| `cashflow-api` | دریافت‌ها، چک و سفته و مراحل آن، انواع چک و دسته چک، اسناد خزانه‌داری | `running` |
| `reference-api` | اطلاعات پایه (ارز، کشور/استان/شهر، بانک‌ها، کدینگ و تفصیل، عطف، گروه بابت و بابت، واحدهای سازمانی، درخواست‌کنندگان، ذینفعان) | `running` |
| `accounts-api` | حساب‌های بانکی و کارت‌های بانکی شرکت، صندوق‌ها، تنخواه، موجودی اول دوره | `running` |
| `requests-api` | درخواست وجه | `running` |
| `keycloak` | سرور هویت (هنوز به برنامه وصل نیست) | `running` |
| `shell` | پوسته Nuxt (منو و قاب صفحه) | `running` |
| `remote-receipts`، `remote-basic-info`، `remote-requests` | micro-frontendهای دریافت‌ها، اطلاعات پایه و درخواست وجه | `running` |
| `gateway` | Nginx، تنها درِ ورودی: `127.0.0.1:8080` | `running` |

این فهرست با اضافه شدن سرویس‌ها بلندتر می‌شود؛ مرجع همیشه خود `deploy/docker-compose.yml` است.

## ۶. آزمون سلامت (smoke test)

```bash
deploy/smoke-test.sh
```

اگر پورت را عوض کرده‌اید: `deploy/smoke-test.sh http://localhost:8081`

این اسکریپت از راه gateway همه چیز را سرتاسری می‌آزماید: پوسته، micro-frontendها، Keycloak، و هر چهار API همراه با پایگاه داده (یک چک دریافت می‌کند، دوباره می‌خواند و برای وصول واگذار می‌کند). تا آماده شدن سرویس‌ها صبر می‌کند، پس می‌توانید بلافاصله بعد از `up` اجرایش کنید. خروجی درست یک خط است که با `OK:` شروع می‌شود؛ در غیر این صورت `FAIL: ...` می‌گوید کدام بخش کار نکرد.

توجه: هر بار اجرا **یک چک آزمایشی** با نام بانک `SMOKE-TEST` ثبت می‌کند. در ویندوز آن را در Git Bash یا WSL اجرا کنید.

## ۷. دیدن فرم‌ها در مرورگر

**http://localhost:8080** را باز کنید. بالای صفحه سال مالی، نسخهٔ در حال اجرا (شمارهٔ commit و تاریخ؛ فقط وقتی کد از ZIP گیت‌هاب یا `git archive` ساخته شده باشد) و دکمه تغییر زبان (فارسی/English) است و منوی اصلی این‌هاست: داشبورد، اطلاعات پایه، درخواست وجه، پرداخت‌ها، موجودی اول دوره، دریافت‌ها، گزارشات.

در تاریخ نوشتن این راهنما:

- **اطلاعات پایه** (`/basic-info`): همه ۱۴ مورد راهنمای کاربری ساخته شده است؛ از جمله کشور، استان و شهر، نوع ارز، نام بانک‌ها، کدینگ حساب‌ها و تفصیل، حساب‌های بانکی و کارت‌های بانکی، انواع چک و دسته چک‌ها، عطف‌ها، گروه بابت و بابت‌ها، صندوق‌ها، تنخواه، واحدهای سازمانی، درخواست‌کنندگان وجه، ذینفعان و «شابلون اسناد خزانه داری».
- **درخواست وجه** (`/payment-requests`): فرم درخواست وجه، تأیید و ابطال آن، و فهرست درخواست‌های تأییدشده برای پرداخت.
- **موجودی اول دوره** (`/opening-balances`): موجودی اول دوره حساب‌های بانکی، صندوق‌ها و تنخواه.
- **دریافت‌ها** (`/receipts`): دریافت نقدی، واریز به حساب بانکی، و دریافت چک و سفته؛ فهرست دریافت‌ها همراه با سند هر دریافت؛ و مراحل چک دریافتی پس از دریافت (واگذاری به بانک، وصول، برگشت، عودت، تعویض، ابطال، خرج).
- **داشبورد**، **پرداخت‌ها** و **گزارشات** فعلاً فقط یک جمله نشان می‌دهند که این بخش در فازهای بعدی اضافه می‌شود. این خطا نیست.

اینکه هر pull request چه چیزی اضافه کرده و چطور باید امتحانش کرد، در توضیح همان PR زیر عنوان **«What changed, in plain words»** نوشته شده است. وضعیت کلی پروژه در `README.md` و `docs/plan.md` است.

نشانی‌های دیگری که برای بازبینی به کار می‌آیند (همه از راه gateway):

| نشانی | چیست |
|---|---|
| `http://localhost:8080/api/reference/banks` | نمونه یک API (فهرست بانک‌ها به JSON). مسیرها: `/api/cashflow/...`، `/api/reference/...`، `/api/accounts/...`، `/api/requests/...` |
| `http://localhost:8080/auth/` | کنسول مدیریت Keycloak. نام کاربری `admin`، رمز: `KEYCLOAK_ADMIN_PASSWORD` در `deploy/.env` |

**هشدار امنیتی:** تا وقتی ورود با Keycloak ساخته نشده، APIها در حالت Development و **بدون احراز هویت** اجرا می‌شوند. به همین دلیل gateway فقط روی `127.0.0.1` گوش می‌دهد. `GATEWAY_ADDRESS` را `0.0.0.0` نکنید و پورت را روی شبکه یا اینترنت باز نکنید.

## ۸. کارهای روزمره

برای کوتاه شدن دستورها، یک بار در هر پنجره ترمینال این تابع را تعریف کنید (در ریشه مخزن):

```bash
dc() { docker compose -f deploy/docker-compose.yml --env-file deploy/.env "$@"; }
```

در PowerShell:

```powershell
function dc { docker compose -f deploy/docker-compose.yml --env-file deploy/.env @args }
```

| می‌خواهید… | دستور |
|---|---|
| وضعیت سرویس‌ها را ببینید | `dc ps -a` |
| لاگ همه سرویس‌ها (۱۰۰ خط آخر) | `dc logs --tail 100` |
| لاگ زنده یک سرویس | `dc logs -f cashflow-api` (خروج: Ctrl+C) |
| سیستم را متوقف کنید (داده می‌ماند) | `dc stop` |
| دوباره راه بیندازید | `dc up -d` |
| بعد از گرفتن کد تازه، دوباره بسازید | `dc up -d --build` |
| **پاک کردن همه داده** آزمایشی و شروع از پایگاه داده خالی | `dc down -v` و سپس `dc up -d --build` |
| داخل پایگاه داده را ببینید | `dc exec postgres psql -U postgres -d treasury` (مثلاً `\dn` برای schemaها، `\dt cashflow.*` برای جدول‌ها، `\q` برای خروج) |
| جای اشغال‌شده Docker را ببینید / حافظه نهان ساخت را خالی کنید | `docker system df` / `docker builder prune` |

داده‌ها در volume به نام `treasury_pgdata` داخل Docker نگه داشته می‌شوند، نه در پوشه مخزن. پس پاک کردن پوشه یا clone دوباره، داده را پاک نمی‌کند؛ فقط `dc down -v` پاک می‌کند.

## ۹. بازبینی یک pull request، گام به گام

1. داده قبلی را پاک کنید تا از حالت تمیز شروع کنید: `dc down -v`
   (اسکریپت‌های پایگاه داده فقط رو به جلو هستند؛ اگر از یک شاخه جدیدتر به شاخه قدیمی‌تر برگردید، جدول‌های شاخه جدیدتر سر جایشان می‌مانند. با پاک کردن، همان چیزی را می‌بینید که CI می‌بیند.)
2. کد PR را بگیرید (بخش ۳): `git fetch origin pull/12/head:pr-12` و `git switch pr-12`
3. بسازید و اجرا کنید: `dc up -d --build`
   اگر خطای `required variable ... is missing a value` آمد، بخش ۴ نکته ۲.
4. آزمون سلامت: `deploy/smoke-test.sh`
5. در مرورگر، طبق بخش «What changed, in plain words» همان PR، کار جدید را امتحان کنید. اگر صفحه قبلاً باز بوده، یک بار با Ctrl+F5 تازه‌اش کنید.
6. لاگ‌ها را برای خطا نگاه کنید: `dc logs --tail 100`
7. در پایان به `main` برگردید: `git switch main` و `git pull`

**آزمون‌های خودکار:** لازم نیست آن‌ها را روی دستگاه خودتان اجرا کنید. روی هر pull request چهار job در GitHub Actions اجرا می‌شود (زبانه *Checks* در صفحه PR): `dotnet` (ساخت و همه آزمون‌های ‎.NET، از جمله آزمون‌های PostgreSQL)، `database-and-migration`، `frontend` و `compose` (همین راهنما به‌صورت خودکار: ساخت، smoke test، توقف و شروع دوباره). اگر می‌خواهید آزمون‌ها را محلی اجرا کنید، ابزارهای لازم و دستورها در `docs/getting-started.md` و جدول «Commands» در `CLAUDE.md` آمده است.

## ۱۰. اگر مشکلی پیش آمد

| نشانه | علت و راه‌حل |
|---|---|
| `required variable ... is missing a value: set in .env` | یک متغیر در `deploy/.env` شما نیست (فایل ساخته نشده، یا نسخه تازه سرویس جدیدی دارد). آن را از `deploy/.env.example` اضافه کنید. |
| `db-init` با `exited (2)` و در لاگ: `password authentication failed for user "postgres"` | `POSTGRES_PASSWORD` بعد از اولین اجرا عوض شده، یا volume از اجرای دیگری (مثلاً فایل‌های دوبار کلیک ویندوز) مانده است. رمز قبلی را برگردانید یا `dc down -v`. |
| هنگام شروع `gateway`: `failed to bind host port 127.0.0.1:8080/tcp: address already in use` (در بعضی نسخه‌های Docker: `port is already allocated`) | پورت ۸۰۸۰ اشغال است. در `deploy/.env` مقدار `GATEWAY_PORT` را عوض کنید و `dc up -d`. |
| مرورگر: `502 Bad Gateway` درست بعد از شروع | سرویس‌ها هنوز در حال بالا آمدن هستند. ۳۰ تا ۶۰ ثانیه صبر کنید یا `deploy/smoke-test.sh` را اجرا کنید که خودش صبر می‌کند. |
| مرورگر: «This site can't be reached» | سیستم اجرا نیست (`dc ps -a`) یا پورت دیگری در `.env` تنظیم شده است. |
| تغییرهای PR را نمی‌بینید | `--build` فراموش شده، یا شاخه اشتباه است (`git log --oneline -1`)، یا مرورگر نسخه قبلی را نگه داشته (Ctrl+F5). |
| دانلود image یا بسته با `403`، `timeout` یا `connection reset` می‌ایستد | شبکه شما به یکی از نشانی‌های بخش ۱ دسترسی ندارد. برای Docker Hub می‌توانید در تنظیمات Docker یک registry mirror بگذارید (*Settings → Docker Engine → `registry-mirrors`*)؛ برای بقیه به اتصال بدون محدودیت نیاز است. اگر فقط `quay.io` در دسترس نیست، همان image Keycloak روی Docker Hub هم هست: `docker pull keycloak/keycloak:26.4` و سپس `docker tag keycloak/keycloak:26.4 quay.io/keycloak/keycloak:26.4`. |
| ساخت با `Killed` یا کمبود حافظه می‌ایستد | به Docker حافظه بیشتری بدهید (Docker Desktop: *Settings → Resources*، دست‌کم ۶ گیگابایت) و دوباره `dc up -d --build`. |
| `no space left on device` | `docker system df` را ببینید؛ `docker builder prune` و `docker image prune` جا باز می‌کنند. |
| در لاگ APIها: `Cannot load library libgssapi_krb5.so.2` | بی‌خطر است. درایور PostgreSQL کتابخانه Kerberos را پیدا نمی‌کند و بدون آن با رمز وصل می‌شود. |
| ویندوز: خطاهایی مثل `$'\r': command not found` یا `bash\r` | فایل‌ها با پایان خط ویندوزی گرفته شده‌اند. دوباره با `git clone -c core.autocrlf=false ...` بگیرید (بخش ۱). |
| لینوکس: `permission denied ... /var/run/docker.sock` | کاربر شما عضو گروه `docker` نیست؛ دستورها را با `sudo` اجرا کنید یا کاربر را به گروه اضافه کنید. |

**روش جایگزین در ویندوز:** بعد از clone می‌توانید به‌جای بخش‌های ۴ تا ۶ روی `deploy\windows\Start Treasury.cmd` دوبار کلیک کنید (همان روش مالک محصول، `docs/test-server-windows.md`). این روش تنظیمات را خودش می‌سازد و در `%LOCALAPPDATA%\Treasury\treasury.env` نگه می‌دارد، نه در `deploy/.env`. روی یک دستگاه **فقط یکی از دو روش** را به کار ببرید، چون هر دو از یک پایگاه داده (volume `treasury_pgdata`) استفاده می‌کنند و رمزهایشان با هم فرق دارد.

**گزارش مشکل:** اگر با این جدول حل نشد، این سه خروجی را در یک comment روی همان pull request (یا یک issue) بگذارید. پیش از فرستادن مطمئن شوید رمزی در متن نیست.

```bash
git log --oneline -1
docker compose -f deploy/docker-compose.yml --env-file deploy/.env ps -a
docker compose -f deploy/docker-compose.yml --env-file deploy/.env logs --tail 100
```

</div>
