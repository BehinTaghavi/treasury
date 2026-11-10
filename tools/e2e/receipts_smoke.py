"""Browser smoke test of the shell + receipts micro-frontend + cashflow API (all running locally).

Start: the API (port 5081, Development), `pnpm preview` in frontend/remotes/receipts (5174), `pnpm dev` in frontend/shell (3000).
Run:   python tools/e2e/receipts_smoke.py screenshot.png   (needs: pip install playwright; a Chrome/Chromium)
"""
import os
import sys
from playwright.sync_api import sync_playwright

with sync_playwright() as p:
    b = p.chromium.launch(executable_path=os.environ.get("CHROME_PATH", "/usr/bin/google-chrome"), args=["--no-sandbox"])
    page = b.new_page(viewport={"width": 1366, "height": 860}, locale="fa-IR")
    logs = []
    page.on("console", lambda m: logs.append(f"{m.type}: {m.text}"))
    page.goto("http://localhost:3000/receipts", wait_until="networkidle")
    page.wait_for_selector(".tr-form", timeout=20000)
    f = page.locator(".tr-form")
    def fill(i, v):
        f.locator("input").nth(i).fill(v)
    fill(0, "778899"); fill(1, "بانک ملت"); fill(2, "۲۵٬۰۰۰٬۰۰۰"); fill(3, "1405/08/18"); fill(4, "1405/09/30"); fill(5, "شرکت الف"); fill(6, "فروش محصولات")
    f.locator("input").nth(6).press("Control+Enter")
    page.wait_for_selector(".tr-table tbody tr")
    print("row:", page.locator(".tr-table tbody tr").first.inner_text().replace("\t", " | "))
    print("msg:", page.locator(".tr-msg").inner_text())
    page.get_by_role("button", name="واگذاری جهت وصول").click()
    page.locator(".tr-action input").nth(1).fill("ملت - جاری ۱۰۰")
    page.get_by_role("button", name="تایید").click()
    page.wait_for_function("document.querySelector('.tr-table tbody tr').innerText.includes('واگذار جهت وصول')")
    print("after deposit:", page.locator(".tr-table tbody tr").first.inner_text().replace("\t", " | "))
    # Same bank + number again: saved with a warning
    fill(0, "778899"); fill(1, "بانک ملت"); fill(2, "1000000"); fill(4, "1405/09/30"); fill(5, "شرکت ب"); fill(6, "فروش")
    f.locator("input").nth(6).press("Control+Enter")
    page.wait_for_selector(".tr-msg.warning")
    print("duplicate:", page.locator(".tr-msg").inner_text())
    # Invalid date: refused before reaching the server
    fill(0, "1"); fill(1, "x"); fill(2, "10"); fill(4, "1405/07/31"); fill(5, "y"); fill(6, "z")
    f.locator("input").nth(6).press("Control+Enter")
    page.wait_for_selector(".tr-msg.error")
    print("invalid date:", page.locator(".tr-msg").inner_text())
    page.screenshot(path=sys.argv[1], full_page=True)
    errs = [l for l in logs if l.startswith("error")]
    print("console errors:", errs[:5])
    b.close()
