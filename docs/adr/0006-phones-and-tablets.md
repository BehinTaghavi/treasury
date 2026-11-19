# ADR 0006 — The system works on phones and tablets

Status: accepted as a business decision (CEO, 19 Nov 2026: "gadget based", meaning devices — for example a manager approving a payment request from a phone). Details marked "to confirm" wait for the owner's answers Q42–Q43. Not built yet.

## Decision

1. **One web application for every device.** The same shell and micro-frontends (ADR 0004) adapt to the screen: desktop, tablet and phone. No separate native app is written at first, so there is one code base to build, test and review.
2. **It can be installed on the home screen** of a phone or tablet like an app (web app manifest, icon, full-screen), without an app store. It always works online: nothing financial is saved on the device and no entry is made offline.
3. **A rule for every screen from now on:** it must be usable at phone width (360 px) and tablet width (768 px), right-to-left: lists turn into cards or scroll inside their own box, forms fall into one column, buttons and list boxes are large enough for a finger, and the main menu folds into a drawer. The shared pieces for this live in `@treasury/ui-kit`; a browser test opens each micro-frontend at phone width.
4. **Designed first for the phone:** the task inbox and approving or rejecting payment requests, the dashboard (bank and cash balances, cheques due), and looking up a cheque, a receipt or a request. **Designed first for keyboard and large screen, but still working on a tablet or phone:** fast data entry (receipts, payments, Basic Information) and cheque printing (which needs the printer's PC).
5. **Safety before reach:** phones mean access from outside the office, so the system is opened to the internet only with login in place, HTTPS only, and a second step (one-time code) for approvers and administrators, as the design document 8.2 already asks.

## To confirm (owner)

- Which jobs must work well on a phone first (Q42).
- Whether people should be told about waiting approvals by SMS or by a notification on the phone, and whether an app in a store is needed later (Q43). If a store app is ever needed, the same web application can be wrapped (for example with Capacitor) rather than rewritten.

## Consequences

- Screens built so far (Basic Information, opening balances, receipts, received-cheque steps, payment requests) were made for a desktop; they get one pass for phone and tablet widths. No business rule or API changes.
- Scans can be taken with the phone's camera on the existing attachment boxes (the browser offers the camera for an image file).
- Combined with ADR 0005, the company switcher and the company's logo must also fit a phone's top bar.
