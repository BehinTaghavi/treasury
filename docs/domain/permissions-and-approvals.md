# Permissions («مجوزها»), approval workflow, electronic signature, users and access levels — proposal

> Farsi version: [permissions-and-approvals.fa.md](permissions-and-approvals.fa.md). Keep both in step when either changes.
> Status: **sections 1 and 2 are built; sections 3–7 are a proposal, waiting for the owner's answers** (questions QP1–QP9 at the end).
> Nothing of sections 3–7 is built before the answers.

The owner's report no. 8 (21 Nov 2026) redesigned «دارندگان امضای مجاز» in Basic Information and ended with five suggestions
(«پیشنهاد ۱ تا ۵»). This note says what was built from the report and answers the five suggestions.

## 1. What the report asked for and what is built

| Report | Built |
|---|---|
| 1. «دارندگان امضای مجاز» becomes two items of Basic Information | «دارندگان امضای مجاز (اسناد بهادار)» and «دارندگان مجوزها (فرم‌ها)» |
| 2. The lower part «قاعده امضاء اسناد بهادار» is removed completely | Removed from the screen, the service and the database (the rules stored before are dropped) |
| 3-الف. Extension inside the organisation, e-mail address | Two new fields on both pages |
| 3-ب. «سمت» chosen from seven choices; with «سایر» a box for the position | A list in the owner's order; «سایر» opens «سمت سازمانی» |
| 3-ج. A place for the scan of the specimen signature | A PNG or JPEG picture of at most 1 MB, shown on the form, replaceable and removable |
| 3-د. Two ticks: «دارنده امضاء مجاز (چک)», «دارنده مجوز امضاء (سفته)» | Two ticks; at least one is ticked |
| Bank accounts: «دارندگان امضای مجاز» of the chosen account | A block on the bank account form: number of signatures (one digit); rows with the signatory (only those ticked «چک»), the kind «امضای ثابت / امضای متغیر» and the scan of the signature; the first row with «تعریف دارنده امضاء مجاز اول», the next with «رکورد جدید» |
| «دارندگان مجوزها»: same upper part, «سمت سازمانی / شغل», «نقش» with five ticks, and the list of treasury operations per role | Built as described; a person may have several roles, each in several operations |

Rules the services check (each has a test):

- A signatory signs cheques, notes or both. One named by a bank account is switched off instead of deleted and keeps its «چک» tick.
- A bank account: the number of signatures is 0–9 (0: not set yet); a signatory is named once, is active and ticked «چک» when it
  is added; **the fixed signatories are not more than the number of signatures** (they all sign every cheque) and **the
  signatories named are not fewer than that number** (otherwise no cheque could be completed). The owner's example — two
  signatures, one fixed and two variable signatories — is a test.
- A permission holder has at least one role; a role is ticked once and has at least one operation; «سایر» needs its title and
  has a place among the fixed steps (last unless chosen). The holders of an operation are listed in the order of the flow.

## 2. What happens to the forms that used the signature rule

Two forms asked the removed rule who signs them: the **receipt of notes** («رسید دریافت سفته») and the **cheque payment sheet**
(«برگ پرداخت چک», printed by «ثبت انتقال / خرج چک»). They now list **the holders of a permission in their own operation**
(«دریافت سفته», «ثبت انتقال (خرج چک)»), in the order of the roles, each with its roles and job title. The user ticks who signs;
**nobody is compulsory and no number is asked for**. The printed form has one signature box per chosen person, headed by the
roles and the job title. Receipts saved before keep the signatories they were saved with. This is a bridge until the workflow
of section 3 is built; a company that defines no permission holders sees the forms as before, without these boxes.

## 3. Suggestion 1 — an approval workflow for the «مجوزها»: yes, and how

**Answer: yes, it can be built**, on top of the page «دارندگان مجوزها» as it is now. It needs login first (each person works
with their own user name and password), so it comes with the Keycloak login of Phase B. Proposed mechanism:

1. **A holder is linked to a user.** The form of a permission holder gets one more field, «کاربر سیستم». A holder without a
   user (not a user of the system) cannot act in the system; for that person the printed form keeps an empty box to sign by hand.
2. **The steps of an operation come from the roles ticked for it**, always in this order: تهیه کننده (who filled the form) →
   تایید کننده (اول) → تایید کننده (دوم) → تصویب کننده (اول) → تصویب کننده (دوم). A step exists only when somebody holds that
   role in the operation — with one or two steps missing the rest keep the order, confirmation before approval — and an
   operation in which nobody holds a role has no workflow. **Confirmed by the owner on 22 Nov 2026**, who added: a role under
   «سایر» that stands between these steps is given its order. That part is built: the form asks «جایگاه در گردش کار» for
   «سایر» (before the first confirmer, or after one of the four steps; last when nothing is chosen).
3. **When several persons hold the same role in one operation, any one of them acts** (the first who does); the others see
   that it is done.
4. **Task inbox («کارتابل»).** After the preparer saves a form it is «در انتظار تایید» and appears in the inbox of the holders
   of the first step. Each of them can **confirm**, **send back for correction** (with a reason; the form returns to the
   preparer and the steps start again) or **reject** (with a reason; the form is closed). After the last step the form is
   «تصویب شده» and takes effect.
5. **What "takes effect" means** depends on the operation: a payment request becomes payable; a payment can be made and its
   cheque printed; for a receipt — the money or the cheque has already arrived — the form is recorded at once and the steps
   confirm it afterwards, and its accounting voucher can be confirmed only after the last step (QP3).
6. **Every act is recorded**: who, in which role, date and time, remark. The history is shown on the form and printed at its
   foot as a row of boxes — تهیه کننده, تایید کننده (اول), … — each with the name, the job title, the date and time and, if a
   scan is stored, the picture of the signature. **A signature picture is printed only for a person who really acted in the
   system with their own login**, never merely because a scan exists.
7. **A form that is already confirmed cannot be corrected.** It is sent back first; that removes the later confirmations.
8. **Phones** (ADR 0006): the inbox and the confirm / send back / reject buttons are designed for the phone first, so a manager
   can approve from a phone.

Order of building proposed: login and users → payment requests (their present single «تایید» becomes the steps) → payments →
receipts and the steps of cheques and notes.

## 4. Suggestion 2 — electronic signature or scans of signatures: my recommendation

**Recommendation: the act recorded by the system is the signature; the scan is only its picture, and optional.**

- What proves that a person confirmed a form is the system's record: that person logged in with their own password and pressed
  «تایید» at a known date and time, and the record cannot be changed afterwards. This is the electronic signature of the
  internal forms. It needs no scan.
- The scan is kept as an **optional picture** printed in the person's box, so that the printed form looks like the forms the
  company and its auditors are used to. A company that does not want scans simply uploads none: the box then shows the name,
  the role, the date and time and the words «به صورت الکترونیکی تایید شد».
- For a stronger proof on sensitive steps (for example approving a payment above an amount the company sets), the system can
  ask for a **one-time code sent by SMS** to the person's mobile at the moment of approval (QP5). This needs an SMS provider.
- **Not proposed:** certified digital signatures with hardware tokens (گواهی امضای دیجیتال). They add cost and a token for
  every approver and bring nothing for the company's internal forms. Cheques and notes themselves are still signed on paper
  and registered in Sayad by the authorised signatories; the system does not sign them.
- One point is not mine to decide: whether the company's **auditor and legal adviser** accept forms confirmed only in the
  system, without a handwritten signature. I suggest asking them once; until then the printed form can still be signed by hand.

So the scans of the two pages built now stay useful and optional, and nothing built has to be undone whichever way you decide.

## 5. Suggestion 3 — «مجوزها» as the company's choice (plug and play)

**Agreed, and the design already works that way:**

- Today: a company that enters nobody in «دارندگان مجوزها» has no signer boxes and no extra step on any form.
- With the workflow: one switch per company, «گردش کار مجوزها: فعال / غیرفعال», in the company's settings, and under it the
  choice per operation follows from the roles — an operation without a holder has no steps. A company can therefore start
  with, say, only payment requests and add operations later, without anyone changing the program.
- With several companies (ADR 0005) each company has its own switch, its own permission holders and its own steps.

## 6. Suggestion 4 — users and access levels with several companies: the form I propose

Two different things are kept apart: an **access level** says which screens a user may open and what they may do there; a
**«مجوز»** says which forms wait for that person's confirmation or approval. A person can have one without the other.

**Page «کاربران» (a new item «مدیریت سیستم» of the main menu, seen only by administrators):**

| Field | Note |
|---|---|
| نام و نام خانوادگی، نام کاربری | One user name per person, for all the companies of a group |
| تلفن همراه، ایمیل | For the one-time code and for notices |
| وضعیت | فعال / غیرفعال (a user who left is switched off, never deleted: their name stays on old forms) |
| شخص مرتبط | The person's record in «دارندگان مجوزها» / «دارندگان امضای مجاز», so the roles and the scan of the signature belong to the user |
| شرکت‌ها و گروه دسترسی | One row per company the user works in, each with its access group (rights are set company by company, QT5) |
| محدودهٔ داده (اختیاری) | Limit to some bank accounts, cash desks or organisational units; empty: all |

Passwords are not kept in the Treasury system: Keycloak keeps them, with the rule for their length and renewal, and can ask
for a second step at login.

**Page «گروه‌های دسترسی»:** a group is a table — rows: the areas of the menus (اطلاعات پایه, موجودی اول دوره, درخواست وجه,
دریافت‌ها, پرداخت‌ها, اسناد, گزارشات, مدیریت سیستم); columns: مشاهده, ثبت, ویرایش, حذف, چاپ, تایید سند. The system comes
with ready groups that the company's administrator can copy and change:

| Ready group | Meant for |
|---|---|
| مدیر سیستم شرکت | Users, access groups, Basic Information of the company |
| خزانه‌دار | Everything of receipts and payments, with «تایید سند» |
| کاربر دریافت / کاربر پرداخت | Recording receipts / payments, without «تایید سند» |
| درخواست‌کننده | Writing and following their own payment requests |
| مدیر (مشاهده و تصویب) | Seeing everything, the task inbox, the reports; no recording |
| حسابرس / مشاهده‌گر | Seeing and printing only |

**Who manages whom** (already decided, QT4): our staff create a client and its companies; a group has a group administrator
over all its companies; each company has its own administrator who manages that company's users, groups and «مجوزها».
Every login and every change of a user's rights is recorded with who made it and when.

## 7. Suggestion 5 — the preparer's name at the foot of every form

**Agreed.** Today a form already keeps «کاربر ثبت‌کننده», but without login it is not a real person. With login, every printed
form gets the box **«تهیه کننده»** as the first box at its foot: the user's name, the date and time of recording and — when
the user is linked to a person whose scan is stored — the picture of the signature. Built with the login, for every form at once.

## 8. Questions

Asked on 22 Nov 2026. The questions about what is already built (Q86–Q93) are in [open-questions.md](open-questions.md).

| # | Question | Proposed |
|---|---|---|
| QP1 | Is the **order of the steps** right: تهیه کننده → تایید کننده (اول) → تایید کننده (دوم) → تصویب کننده (اول) → تصویب کننده (دوم) → «سایر»? Where do the roles under «سایر» stand? | **Answered 22 Nov 2026:** yes, that order; with steps missing, confirmation first, then approval; «سایر» gets its own order among the steps — built as «جایگاه در گردش کار» on the form; choosing the place (before / after a step) instead of typing a number is enough (owner, the same day). |
| QP2 | When **several persons hold the same role** in an operation: is one of them enough, or must all of them act? | One of them |
| QP3 | **Receipts:** the money has already arrived when the form is filled. Is it right that a receipt is recorded at once and confirmed afterwards, and that only its accounting voucher waits for the last step? And for payment requests and payments: nothing leaves before the last approval? | Yes to both |
| QP4 | Are **amount limits** needed — for example the second approver only above an amount — and a **substitute** («جانشین») for a holder who is away? | Both, set by the company; not in the first version |
| QP5 | **Electronic signature:** do you accept the recommendation of section 4 — the system's record is the signature, the scan is an optional picture, a one-time SMS code for sensitive approvals, no hardware tokens? Will you ask the auditor / legal adviser whether forms confirmed only in the system are accepted? | As recommended |
| QP6 | **Switch per company** «گردش کار مجوزها» and steps per operation from the roles — is that the "plug and play" you mean? | Yes |
| QP7 | Is the form of **users and access groups** of section 6 what you need? Which ready groups should be added or dropped? Is the limit by bank account / cash desk / organisational unit needed in the first version? | As proposed; the limit later |
| QP8 | The same person is often **both a signatory and a permission holder** (for example the managing director). Now the person is entered on each page, with the scan twice. When users are built, should the two pages take the person from one common record? | Yes, with the users |
| QP9 | **Order of building** after your answers: login and users → workflow of payment requests → payments → receipts. Or do you want «درخواست وجه» and «پرداخت‌ها» reviewed first, as planned, and the workflow after them? | Review first, then login and workflow |
