# Your own Windows PC as the Treasury test server

For the product owner. No programming needed: you install one free program, download the system, and double-click.
The system then runs on your PC and you use it in your browser at **http://localhost:8080**.
Only your PC can open it. The system has no login yet, so it is deliberately not reachable from other computers.

> **Test data only.** Do not type real Iran Nazho Co. data into the test system.

## 1. Check your PC (2 minutes)
- Windows 10 or 11, 64-bit.
- At least 8 GB of memory (16 GB is more comfortable). Check: *Settings → System → About → Installed RAM*.
- At least 20 GB of free disk space.

## 2. Install Docker Desktop (once, about 15 minutes)
Docker Desktop is the free program that runs the system's parts (database, login server, program, screens) on your PC.

1. Open https://docs.docker.com/desktop/setup/install/windows-install/ and click **Docker Desktop for Windows – x86_64**.
2. Run the downloaded installer. If it asks how to install, choose **All-users installation** and click **Yes** when Windows asks for permission
   (it needs to switch on the Windows feature *Virtual Machine Platform*; a per-user installation cannot).
   Keep the other ticks it suggests (including **Use WSL 2**) and click **OK**.
3. Restart the PC when it asks.
4. Open **Docker Desktop** from the Start menu. Accept the agreement. You can **skip** signing in and the survey.
5. Wait until the bottom-left corner of Docker Desktop says **Engine running** (green).

If the installer says the *Virtual Machine Platform* feature is not enabled: open the Start menu, type **PowerShell**, right-click
**Windows PowerShell → Run as administrator**, type `wsl --install`, press Enter, restart the PC and run the installer again.
If Docker Desktop shows another message about *WSL* or *virtualization*, take a screenshot and send it to Claude. These are one-time Windows settings, and Claude will tell you exactly what to click.

*Licence:* Docker Desktop is free for companies with fewer than 250 employees **and** less than USD 10 million yearly revenue.
If the company is bigger, use **Rancher Desktop** instead (free, open source, https://rancherdesktop.io).
In its first-start screen, choose the container engine **dockerd (moby)** and untick **Kubernetes**. Everything below works the same.

## 3. Download the Treasury system
1. While signed in to GitHub in your browser, open this link. It downloads the newest approved version as a ZIP file:
   https://github.com/BehinTaghavi/treasury/archive/refs/heads/main.zip
   (Until the first pull request is merged, Claude gives you a link to the version under review instead.)
2. In your Downloads folder, **right-click the ZIP → Properties**. If there is an **Unblock** tick box at the bottom, tick it and click **OK**.
   This stops Windows from blocking the start files.
3. **Right-click the ZIP → Extract All…** (with WinRAR: **Extract files…**) and extract it into **`C:\Treasury`**.
   Avoid *Documents* and *Desktop*: on many PCs they are synced with OneDrive, which can get in Docker's way.

## 4. Start it
1. In the extracted folder, open **deploy → windows**.
2. Double-click **Start Treasury**. A black window opens and shows progress.
   - **The first time takes 10–20 minutes.** It downloads about 2 GB and builds the system. Later starts take under a minute.
   - On a slow internet connection a download can break off. The window then says *Trying again* and continues where it stopped;
     you do not need to do anything. If it still fails after 3 tries, just double-click **Start Treasury** again later. What was already downloaded is kept.
   - If Windows asks *"Do you want to allow…"* for Docker, click **Allow**.
3. When the window says **Treasury is running at http://localhost:8080**, your browser opens the Receipts screen.
   You can close the black window; the system keeps running.

To open it again later, type **http://localhost:8080** in the browser, or bookmark it.

## 5. What you can try today
Only the **Receipts** screen (دریافت‌ها) works so far. Suggested checks:
1. Receive an ordinary cheque (number, bank, amount, received date like `1405/08/19`, due date, drawer, purpose). Press **ثبت** or Ctrl+Enter.
2. In the list, choose **واگذاری جهت وصول** (deposit for collection), then mark it **cleared**. Watch the status change.
3. Receive another cheque and mark it **bounced** after depositing it. Try **return** (عودت) and **endorse** (خرج) on others.
4. Enter a cheque with the **same bank and number** twice: you should get a duplicate warning, and it is still saved (open question Q1).
5. Type an impossible date like `1405/13/40`: you should get a clear error and nothing is saved.

Anything that behaves differently from how your company works, or from the old program, is exactly what Claude needs to hear.
Write it down in plain words (Farsi is fine), with a screenshot if you can.

## 6. Every day
| You want to… | Double-click (in *deploy\windows*) |
|---|---|
| Start the system | **Start Treasury** |
| Stop it (frees memory; your test data is kept) | **Stop Treasury** |
| Delete all test data and begin again empty | **Reset test data** (asks you to type YES first) |

The system also stops when you shut down the PC. After a restart, double-click **Start Treasury** again.

## 7. Getting a newer version
After a pull request is merged, download the ZIP again (step 3), extract it to a **new** folder and double-click **Start Treasury** there.
Your test data is kept. It is stored inside Docker, not in the folder, and the passwords are kept in
*%LOCALAPPDATA%\Treasury\treasury.env*. Do not delete that file. You can delete the old folder.

## 8. If something goes wrong
- The black window shows a **red message**: take a screenshot of the whole window and send it to Claude.
- **"Docker Desktop is not installed"**: do step 2.
- **"Docker is not running"**: open Docker Desktop, wait for *Engine running*, and try again.
- **"Docker could not build the system after 3 attempts"**: usually the internet connection. Check it, then double-click **Start Treasury** again. Each try continues from what was already downloaded.
- **"Conflict. The container name ... is already in use"**: an earlier start was interrupted. Since 14 Nov 2026 **Start Treasury** clears this
  by itself; with an older download, use a newer one (step 7). Your test data is kept.
- The browser says **"This site can't be reached": the system is not started. Double-click **Start Treasury**.
- The PC becomes slow: use **Stop Treasury** when you are not testing.

## For the reviewer (technical)
`deploy/windows/treasury.ps1` (Windows PowerShell 5.1) wraps `docker compose -f deploy/docker-compose.yml`. Its `.env` lives in
`%LOCALAPPDATA%\Treasury\treasury.env` with generated passwords and `GATEWAY_ADDRESS=127.0.0.1`. It waits for the API (404 on an
unknown id) and the shell (200) before opening the browser. `reset` is `down -v`.
`start` runs `compose build` up to 3 times, then `up -d --remove-orphans`. Before that it removes containers named `<12 hex>_treasury-*`
(left by an interrupted recreate, which makes `up` fail with a name conflict); if `up` still fails it runs `down --remove-orphans`
(no `-v`, data volume kept) and tries `up` once more. Builds keep downloads in BuildKit cache mounts (`treasury-pnpm-store`,
`treasury-nuget`, `sharing=locked`), so a retry after a broken download continues where it stopped. The frontend install step comes before
`ARG APP`, so the shell and remote images share one install layer.
