<img src="Dat2DTR/Assets/Dat2DTR-wordmark.png" alt="Dat2DTR - Daily Time Record Maker" width="420">

# Dat2DTR

Offline Windows program that turns the `.dat` file exported from the biometrics device into a
**Daily Time Record (Civil Service Form No. 48)**, with **Late** and **Undertime** computed automatically.
It also deducts filed **leaves**, Late and Undertime from the leave credits and shows the result as a
**Computation of Leaves** (a leave card).

No internet, no database and no extra libraries are needed.

## How to use it

1. **Open .dat File...** - choose the file exported from the biometrics device (or drag it into the window).
2. **Choose the year and the month** (the month list shows the months of the year picked beside it).
3. Click an employee on the left to see the DTR, then **Print...** or **Save To...** (PDF or Excel).

If an employee filed a leave, click **Edit Leave...** before printing (see below).

To print or save several employees at once, tick the boxes beside their names first.

The `.dat` file only has ID numbers. Click **Employee Names...** once, type the name for each ID and
choose the person's work schedule (or import a list: `ID,Name,Schedule`). The program remembers them.

To delete employees (for example someone who left), tick their **Delete** box in Employee Names, click
**Delete ticked...**, answer the two questions (the second can be skipped from then on with *Remember my choice*;
*Ask a second time before deleting*, below the table, turns it back on), then **Save**. Everything about them is erased (name, schedule, leaves, balances, CTO,
Solo Parent Leave, times typed in, Flexi-Time and kept scans); a backup is saved first in Documents
(`Dat2DTR-before-delete-....zip`) so it can be undone with Backup / Restore.

## Four work schedules

Every employee follows one of four schedules. Pick it above the DTR or in **Employee Names...**

| | Office hours | Working days |
|---|---|---|
| Schedule A | 7:00 AM - 6:00 PM | Monday to Thursday |
| Schedule B | 8:00 AM - 5:00 PM | Monday to Friday |
| Schedule C | 7:00 AM - 4:00 PM | Monday to Friday |
| Schedule D | 9:00 AM - 6:00 PM | Monday to Friday |

**Fridays out inspecting schools:** keep the person on their Monday-to-Friday schedule. On a Friday spent
visiting schools, file **Official Business (school visit)** in Edit Leave (whole day, or half day if they scanned
in the morning first). The DTR then shows `OFFICIAL BUSINESS` instead of ABSENT and nothing is deducted.
For a group, tick them and use Edit Leave for the ticked employees.

New employees start on Schedule A. The hours, lunch break, grace period and working days of each
schedule can be changed in **Office Hours...**

## The months of every file opened

The month is chosen with two lists: the **year** (every year that has scans) and the **month** (the months of that
year, newest first), so the list stays short however many years are kept.

Every `.dat` file opened is **kept** on this computer, month by month, in `%AppData%\Dat2DTR\scans`
(`2026-08.dat`, `2026-09.dat`... in the device's own layout, plus `covered.csv`: the dates each file covered).
So after opening the August file and then the September file, both months are in the month list, and the
kept months are there again the next time the program starts, without opening a file. Opening a file only
**adds**: the same scan in two files is kept once, and nothing typed in, filed or computed is ever lost
(those are kept in their own files - see below). A day no opened file covered stays blank (never ABSENT).
The kept scans are in the backup too. The `.dat` files themselves are never changed.

## Version and About

The **clock** at the bottom right of the main window shows the date and time in the Philippines (UTC+8), from the
computer's clock (so it is right even when Windows is set to another time zone). It is only shown on screen.

**About** (top right) shows the version (also in the title bar, e.g. `Dat2DTR 1.7.0`), the developer and how to
contact them, and the version history. The version is always the newest entry of the history in `Core/AppInfo.cs`;
for a new version add an entry at the top there and put the same number in `Dat2DTR.csproj` (`Version`).

## Holidays

**Holidays...** (below the list) keeps the holidays of the year, used for every employee: the DTR says
`HOLIDAY` instead of ABSENT and nothing is deducted. The window has two tables, **Regular Holidays** and
**Special (Non-Working) Days**. **Add** fills in the Philippine holidays whose dates are
fixed by law (Holy Week is worked out for the year); the ones declared by Proclamation (Chinese New Year,
Eid'l Fitr, Eid'l Adha, extra special days) are typed in by hand. A leave filed in Edit Leave comes first.
Kept in `%AppData%\Dat2DTR\holidays.csv`.

## Finding and checking quickly

- The list beside the search box shows everyone, or only the employees that **need checking (!)**,
  were **typed in by hand (\*)** or have a **leave (L)**.
- In the preview (on screen only, never printed) days to check are light red, days typed in by hand light
  yellow, and days with Late / Undertime typed in light orange.
- **Next >** and **< Previous** in Edit DTR and Edit Leave save and open the employee below / above in the list.
- Cancel or X after a change asks before throwing the change away.

## Edit DTR

Edit DTR and Edit Leave open maximized (filling the screen); "Restore" in the title bar gives the normal size.

**Edit DTR...** (above the DTR) opens a table with one row per day. Use it to type in a scan that
was forgotten, correct a wrong time, or type a remark such as `HOLIDAY` on a day with no
times (it is printed in place of ABSENT). Late and Undertime are computed again from what was typed.

The **Late** and **Undertime** of a day can also be typed in (for example `0` when work was suspended at
3:00 PM and everyone went home at 3:00). The table has the same four columns as the DTR - **LATE Hours,
LATE Minutes, UNDERTIME Hours, UNDERTIME Minutes** - so type the hours and minutes there (`0` for none), plus a **Reason**
(required). The DTR prints the typed value, the totals and the Computation of Leaves use it, the computed
value stays visible in the Result column, and every changed day is listed in the yellow message.
Empty both boxes (Hours and Minutes) to go back to the computed value.

**Flexi-Time** (a tick box per day; click the heading to tick or untick every working day): the person may work the
hours of another schedule on that day, for example 7:00 AM - 4:00 PM instead of 8:00 AM - 5:00 PM. The program tries
every schedule with the same working hours (8 hours for B, C and D; lunch not counted), uses the one with the least late +
undertime (a tie keeps the person's own schedule), and writes it in the Remark column, e.g. `Flexi-Time: Schedule C (7-4)`.
7:00 - 4:00 on Schedule B -> Schedule C, not late, no undertime. 7:30 - 4:30 -> stays on B (30 minutes undertime either way).
Schedule A (10 hours) has no other schedule of the same length. A day that is not ticked follows the person's own schedule.

**No Late / Undertime** (a tick box per day, beside Flexi-Time): no Late and no Undertime are counted on that day,
for example a Friday when everyone went back to 7-4 or 8-5. The times stay on the DTR; the Late / Undertime boxes are left
empty, not totalled, not deducted and not in the Reports, and the Result column shows what would have been counted.
It does not change ABSENT (a working day with no scans).

**Forgotten lunch scans** are fine: a day with only the arrival and the departure (or one lunch scan) is judged on the
arrival and the departure, with no warning.

The `.dat` file is never changed. Typed values are kept in `%AppData%\Dat2DTR\corrections.csv`
(times) and `adjustments.csv` (Late / Undertime, Flexi-Time and No Late / Undertime), and days typed in by hand are listed on screen
(the employee gets a `*` in the list).

## Edit Leave and the Computation of Leaves

**Edit Leave...** (above the DTR) opens a table with one row per day. On the day of the leave, pick the
kind of leave and whether it was a whole day or half a day. The first time, also type the employee's
**balance brought forward** (Vacation and Sick Leave, from the leave card). After that the balance is
carried over from month to month automatically. Below them, **Special** and **Wellness** take the Special Privilege Leave and
Wellness Leave *left* at the start of the month (for days used before the program was used; e.g. 2 SPL used = type 1).
Empty = counted from the SPL and WL filed this year. Centred below them, **Force/Mandatory** takes the Mandatory / Forced
Leave left in the same way (5 days a year; e.g. 2 days taken = type 3). The typed numbers are kept in `leave-ledger.csv`
and count for the rest of that year (all three start again every January 1).

| Kind of leave | What happens |
|---|---|
| Vacation Leave | Deducted from the Vacation Leave credits |
| Mandatory / Forced Leave | Deducted from the Vacation Leave credits; counted (5 days a year) |
| Sick Leave | Deducted from the Sick Leave credits; if not enough, the rest comes from Vacation Leave |
| Special Privilege Leave | Not deducted; counted (3 days a year) |
| Wellness Leave | Not deducted; counted (5 days a year) |
| CTO | Taken from the CTO given (at most 15 days at a time, each with its own expiry date; unused CTO expires). Can only be filed when there is enough CTO on that day |
| Solo Parent Leave | Taken from the Solo Parent Leave given, the same way as CTO (at most 7 days at a time, each with its own expiry date; unused days expire and are taken off the balance) |
| Holiday | Not a leave: not deducted, not counted; the DTR shows `HOLIDAY` |
| Suspended (work suspension) | Not a leave: not deducted, not counted; the DTR shows `WORK SUSPENDED` |
| Official Business (school visit) | Out of the office for work: counted as present, not deducted; the DTR shows `OFFICIAL BUSINESS` |

Two tick boxes go with these: **WFH** (work from home; on a Suspended day the heading reads *Is it WFH?*)
and **IDLAR**, which can only be ticked after WFH. With IDLAR the day counts as a day at work and the DTR shows
`WORK FROM HOME` (or `HOLIDAY - WORK FROM HOME`). A Suspended day with WFH shows `PD - WFH`, and `PD - WFH/IDLAR`
once IDLAR is ticked. WFH on a day with no leave is an
ordinary working day worked from home; without IDLAR it stays ABSENT and is listed to check. A Holiday can be
saved without WFH or IDLAR.

**Work suspended from a time:** for the ticked employees choose Suspended and How long = *From a time* (for example
3:00 PM). The day is ordinary until then and no undertime is counted after it. For one employee, pick *From 3:00 PM*
under How long in Edit Leave. Leaving before the suspension time is undertime, counted up to it - also when the time is in the
lunch break (suspended from 1:00 PM, out at 12:26 PM = 34 minutes).

**The same leave for many employees:** tick them in the list, click **Edit Leave...** and choose *The ... I ticked*.
Pick the days, the leave (for example Holiday), how long and WFH / IDLAR, then **Apply**. Days that are not working
days in an employee's schedule are skipped.

How the month is computed (Civil Service Commission rules on leave):

- 1.25 Vacation Leave and 1.25 Sick Leave credits are **earned** every month.
- One whole day of leave costs *working hours / 8*: **1.25** on Schedule A (10 hours), **1** on Schedules B, C and D (8 hours).
  Half a day costs half of that.
- The month's **Late** and **Undertime** are converted to a fraction of a day with the CSC table
  (8 hours = 1 day, 1 hour = .125, 1 minute = .002) and deducted from Vacation Leave.
- What the Vacation Leave credits cannot cover is **without pay**, and with days without pay less than 1.25 is
  earned that month (CSC table of computation for leave without pay). The credits never go below zero: the
  computation shows, with a minus sign, what the balance would have come to (`Not enough credits: the balance
  would be -1.812`), then the days without pay that make up for it, so the lines add up to the balance of 0.000.
- **Net** = balance brought forward + earned - enjoyed.

On the DTR, a whole day of leave is printed in place of ABSENT (for example `VACATION LEAVE`). With a
half-day leave, the half on leave is excused: no late or undertime is counted for it.

The **Computation of Leaves** (button above the DTR) shows the leave card of the year - VACATION: Earned,
Enjoyed, Net, Leave Date/s, SPL, WL, CTO, SOLO, Late, Undertime; SICK: Earned, Enjoyed, Net, Leave Date/s - and
how the open month was computed: Vacation and Sick Leave, then Special Privilege Leave, Wellness Leave, CTO and
Solo Parent Leave. Under the table, CTO and SOLO show the days used this year and the balance left. Below a line
across the page, the **History of Availment of Leave** lists the leaves used in every month, January to December,
with the days they were used and the total days.

**CTO given...** in Edit Leave keeps the CTO given to the employee: date, days and expiry date.
**Solo Parent given...** does the same for Solo Parent Leave. In both, typing the date given fills in the
expiry date by itself: one year (Sep 15, 2026 -> Sep 14, 2027); it can be typed over. Each line shows
what is **Left** and its **Status** (Active, Used up, Expired); used-up and expired lines are grey and locked,
kept as a record. CTO and Solo Parent Leave cannot be filed in Edit Leave without enough balance on that day
(for ticked employees, those without enough are skipped and listed).

**Print...** and **Save To...** first ask what you want: *DTR only*, *Computation of Leaves only*, or *Both*.
**Save To...** can also save the scans of the chosen employees as a new **.dat file**, line for line as the device
wrote them - only the month on screen, its whole year, or every month kept (times typed in with Edit DTR and leaves are
not in it).

The Computation of Leaves shows the **VL** and **SL** left at the end of the month at the top right,
and what was used this year under the table.
In Excel the Computation of Leaves is a sheet named `Leave - NAME`; Net is a real formula.

The DTR has two signature lines: *Signature over Printed Name* at the bottom right, and
another *Signature over Printed Name* in the middle below it.

A day that is ABSENT with no leave filed is **not** deducted; it is listed so a person can decide.
Leaves are kept in `%AppData%\Dat2DTR\leaves.csv` (`ID,Date,Leave,Part,WFH,IDLAR`), the remembered months in `leave-ledger.csv`,
the CTO given in `cto.csv`, the Solo Parent Leave given in `solo-parent.csv`.

## Leave Summary, certificates, reminders and backups

- **Leave Summary...** (below the list): every employee of the month in one table - VL, SL, CTO and Solo Parent
  balance at the end of the month, SPL, Wellness and Forced Leave used this year, and reminders. Sortable;
  **Save to Excel...** saves it.
- **Reports...** (below the list): five attendance reports of the whole office for the month on screen - 1. late at
  Time-In on Mondays, 2. no Time-In on Mondays (absent or afternoon only), 3. both together, 4. late in the month,
  5. undertime in the month - each with how many times, the total and the dates with their times
  (e.g. `Sep 21 - In 7:11, Out 6:05 PM (11m)`). A Monday on leave or a holiday is not
  counted. **Save to Excel...** saves all five, one worksheet each; **Save as PDF...** and **Print...** give the report on
  screen or all five, on sideways (landscape) pages with the headings repeated on every page.
- **Certificate of Leave Credits** (a tab above the DTR, beside Computation of Leaves, to see it as it will be printed
  today; and a choice in Print / Save To): one page per employee with a table of every
  leave - Vacation Leave, Sick Leave and their total, Mandatory / Forced Leave, Special Privilege Leave and Wellness
  Leave left for the year, CTO and Solo Parent Leave - ready to sign, saved as PDF. The paragraphs are justified.
  The names and positions of "Prepared by" and "Certified correct" are changed with **Edit Certificate Details...**
  (beside the Certificate tab, or at the bottom of the Print / Save To choice window) and kept in `settings.ini`; an empty one is blank on the certificate.
  At first Prepared by is MARY GRACE E. SACRAMENTO, Administrative Officer - IV. The line under each signature says Position. As of the end of the month on screen, or the end of the
  month before when that month is not over yet. An unknown balance is left as an empty box.
  At the top is the DepEd logo (`Assets\DepEd-Logo.jpg`, built into the program; made from `DepEd-Logo.webp`, which
  Windows printing cannot read). The heading uses **Old English Text MT** (Republic of the Philippines, Department of Education) and
  **Trajan Pro** (NATIONAL CAPITAL REGION), read from `Dat2DTR\Fonts\*.ttf` (copied next to the .exe when it is
  built), from `%AppData%\Dat2DTR\Fonts`, or from the fonts installed in Windows; the PDF carries them inside.
  They are licensed fonts, so the `Fonts` folder is **not** on GitHub (see `.gitignore`): on a new computer, put the
  two `.ttf` files in that folder. Without them the heading is printed in Helvetica / Arial.
- **Reminders** in the yellow bar: Forced Leave not all used (from November, for employees with 10 or more
  days of VL), and CTO / Solo Parent Leave that expires within 30 days.
- **Backup / Restore...**: all the saved files in one `.zip` with the date in its name
  (`Dat2DTR-backup-2026-10-27.zip`). Restoring saves what is there first in Documents
  (`Dat2DTR-before-restore-...zip`), then puts the backup back and reloads everything.

## Rules used for Late and Undertime

Shown for Schedule A (7:00 AM - 6:00 PM, lunch 12:00 - 1:00 PM, grace period 15 minutes).
Schedules B, C and D work the same way with their own times (for example Schedule D: late after 9:15 AM, undertime before 6:00 PM).
The grace period can be changed per schedule in **Office Hours...**; "how Late is counted" decides whether late after it counts from Time In.

| | Rule |
|---|---|
| Late (morning) | Arriving after 7:15 AM is late, counted from 7:00 AM. |
| Grace period | Arriving 7:01 - 7:15 AM is not late only if the minutes are made up: arriving 7:14 AM means leaving at 6:14 PM or later. Leaving at 6:08 PM = 14 minutes late (no undertime). Not applied when the afternoon is on leave or suspended. |
| Mondays | The grace period never goes past 7:15 AM. A 7:00 AM start keeps it; 8:00 AM (Schedule B) and 9:00 AM (Schedule D) must be met sharp on Mondays (8:01 = 1 minute late). Tuesday to Friday have the full 15 minutes. |
| Flexi-Time | Only on days ticked in Edit DTR (see above). |
| No Late / Undertime | A day ticked in Edit DTR: its times stay, nothing is counted (see above). |
| Late (afternoon) | Coming back from lunch after 1:00 PM, counted from 1:00 PM. |
| Undertime | Leaving before 12:00 PM in the morning or before 6:00 PM in the afternoon. |
| Half day | A half day with no scans at all (when the other half has scans) = 5 hours undertime. |
| Absent | A working day (in the person's schedule) with no scans. Absences are not added to undertime. |
| Friday, Schedule A | Not a working day, but it still says ABSENT when there are no scans (as on the office's DTR). No late or undertime is computed for it. |
| Sat / Sun | Only labelled; no late or undertime is computed. |

Which scan goes in which column: first scan = A.M. Arrival, last scan = P.M. Departure, the two scans
around lunch time = A.M. Departure and P.M. Arrival. Two scans within one minute count as one.
The In/Out code inside the `.dat` file is ignored because people often do not press it.

Days with a missing or extra scan are flagged with a yellow message (and a `!` in the list) so a
person can check them. The program does not guess.

## Trying it out

`Dat2DTR/Samples/` contains made-up data:

- `demo-september-2026.dat` - one month of scans for 8 made-up employees
- `sample-names.csv` - made-up names; load it with **Employee Names... > Import list...**

## Where things are

| File | What it does |
|---|---|
| `Form1.cs` | The main window (buttons, employee list) |
| `Core/AttendanceLog.cs` | Reads the `.dat` file |
| `Core/DtrBuilder.cs` | **The Late / Undertime rules** |
| `Core/WorkSchedule.cs` | One set of office hours (Schedule A, B, C or D) |
| `Core/DtrSettings.cs` | The four schedules and how Late is counted (saved in `%AppData%\Dat2DTR`) |
| `Core/EmployeeDirectory.cs` | The ID -> name and schedule list (saved in `%AppData%\Dat2DTR\employees.csv`) |
| `Core/DtrCorrections.cs` | Times typed in by hand with Edit DTR |
| `Core/HolidayCalendar.cs` | The holidays used for every employee (saved in `%AppData%\Dat2DTR\holidays.csv`) |
| `Core/Leave.cs` | The kinds of leave, the filed leaves and the remembered months |
| `Core/LeaveCalculator.cs` | **The leave rules** - how leave, late and undertime are deducted |
| `Core/LeaveSummary.cs` | The Leave Summary of everyone, and the reminders (Forced Leave, CTO / Solo Parent expiring) |
| `Core/AttendanceReports.cs` | The five attendance reports (Mondays, late, undertime) |
| `Core/LeaveCertificate.cs` | What goes on the Certificate of Leave Credits |
| `Core/DataBackup.cs` | Backup and Restore of the saved files (one .zip) |
| `Core/AppInfo.cs` | The version, the developer and the version history (About) |
| `Core/PhilippineTime.cs` | The date and time in the Philippines (UTC+8) for the clock at the bottom right |
| `Rendering/DtrPageRenderer.cs` | **How the form looks** - used by the preview, the printer and the PDF |
| `Rendering/LeavePageRenderer.cs` | How the Computation of Leaves page looks |
| `Rendering/ReportRenderer.cs` | How the attendance reports look when printed or saved as PDF |
| `Rendering/CertificateRenderer.cs` | How the Certificate of Leave Credits looks |
| `Rendering/TrueTypeFont.cs` | Reads the certificate fonts (.ttf) and finds them (Fonts folder, then Windows) |
| `Rendering/JpegImage.cs` | The DepEd logo of the certificate (a JPEG, drawn in the PDF and when printing) |
| `Export/PdfDocument.cs`, `Export/XlsxWriter.cs` | Write the PDF and Excel files |
| `UI/` | The other windows (Edit DTR, Edit Leave, Edit Leave for ticked employees, CTO / Solo Parent given, Holidays, Office Hours, Employee Names, Leave Summary, Reports, Backup / Restore, How it works) |
| `Assets/` | The logo: `Dat2DTR.ico` (program icon), plus SVG and PNG versions |

## Making the downloadable .exe

```
dotnet publish Dat2DTR\Dat2DTR.csproj -c Release -r win-x64 --self-contained true -p:PublishSingleFile=true -o publish
```

This creates `publish\Dat2DTR.exe`, a single file that runs on any 64-bit Windows 10/11 PC
without installing .NET.
