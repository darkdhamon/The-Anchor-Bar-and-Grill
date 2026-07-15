# The Anchor Bar and Grill

This repository contains The Anchor Bar & Grill website in a Blazor Server application, with a layered domain and infrastructure architecture and a database-backed menu system.

## Current Application Scope

- Guest-facing homepage with a three-column landing-page layout on wide screens, stacking as intro, weekly specials, and upcoming events on mobile, with published publicity-copy content centered between the two sidebars
- Homepage photo carousel using five committed placeholder images in the center column so the public slideshow treatment can be reviewed before gallery-backed wiring lands
- Homepage photo carousel now keeps a fixed `4:3` frame, uses contained placeholder-image scaling so venue photos stay fully visible without crop, starts with captions hidden behind a simple show/hide link, uses Disney-style side arrows with a bottom slide tracker inside the frame, sits higher beside the welcome copy on larger screens, stacks the carousel first ahead of the content once the screen narrows, auto-rotates every five seconds, and pauses while hovered or touched
- Homepage weekly specials now preview recurring specials that are active today or will occur within the next six days, while still respecting lifetime dates and recurring seasonal windows, sorting recurring specials by their next actually available occurrence, and only surfacing upcoming dated specials when they have a real valid occurrence in that preview window, with `Today`, `Now available`, and `Limited-time special` wording based on what is actually active right now
- Homepage weekly-specials sidebar uses the live menu-special feed when it has data and falls back to the site's preview specials when public special data is not ready yet
- Homepage upcoming-events sidebar now prefers repository-backed upcoming event queries and falls back to the site's preview events when the live public event feed is still empty
- Database-backed event catalog foundation with explicit publication state, recurring schedule rules, and repository-backed upcoming-event queries for future public and admin event workflows
- Database-backed public menu page inspired by the existing printed menu
- Fixed public menu tabs for `Breakfast`, `Lunch`, `Dinner`, and `Drinks`
- Structured per-tab menu hours, including after-midnight drink service windows
- Guest-facing menu sidebar for `Breakfast`, `Lunch`, `Dinner`, and `Drinks`, paired with an active-service hours panel
- Shared food catalog where sections define their default service visibility, sections can be grouped under parent sections, food items can belong to multiple sections, and item-level menu visibility can narrow those section defaults while drink items stay inside the Drinks tab
- Special menu items that render inline inside the public menu section they belong to, with specials ordered before the standard section lineup, duplicated into a top-level `Specials` accordion for the active menu when specials exist, and scheduled through either dated ranges or multi-day weekly recurrence
- Optional menu-item image support in the public menu and menu editor, including direct menu-item uploads into the local gallery folder with click-to-enlarge previews
- Optional offer start/end dates for menu items, recurring seasonal month/day windows for annually returning items, and guest-facing `Coming Soon`, `Seasonal`, and `Limited Time` labels derived at runtime
- Database-backed Menu Editor with a left-side workspace rail for dedicated `Food`, `Drinks`, and `Hours` views, plus workspace-specific right-side quick guides, collapsible section-tree browsing with header-click expand/collapse behavior, item-backed special offers, parent/child sections, chip-style visibility and scheduling controls, section callouts, archive-aware filters, content filters, duplicate-name protection, and service-hour management
- Database-backed public events page that lists live upcoming event dates, expands recurring schedules into their next published occurrences, supports optional event images, loads the first 90 days immediately, and continues loading later published dates as guests scroll
- About mockup page for the restaurant story and guest experience
- Contact mockup page for location, phone, hours, dynamic social media links, and guest inquiry layout
- Role-gated editor pages for managing menus, events, publicity content, and contact details
- Publicity Editor now includes a repository-backed homepage intro workflow with separate draft and published copy, longer multi-paragraph welcome-message support, a grouped About-page placeholder route under `/admin/publicity`, and UTC-rendered draft/publish timestamps so staff review one consistent audit clock
- Contact editor mockup supports adding, editing, and deleting multiple social media profiles for the public contact page
- Event editor mockup with date/time inputs, richer recurring-event controls, optional images, descriptions, and combo-select promo badges
- Admin-only Help page organized by subject and role type for staff onboarding, editor ownership, bootstrap behavior, and security configuration
- Admin-only User Management page for maintaining staff profile details, setting new temporary passwords for existing users, confirming staff account access, reviewing email-verification status, and assigning or removing `Admin`, `EventManager`, `MenuManager`, and `IT`
- Admin-only Security page for reviewing bootstrap coverage and changing the fallback confirmed-account setting in `appsettings.json`
- Self-service Manage Account profile page where each signed-in user can update their own first name, last name, and phone number
- IT-only placeholder system page that reserves a dedicated technical surface for future diagnostics and tooling
- Shared light and dark themes using the menu-inspired nautical palette

## Staff Access And Security

- Staff accounts are created only by admins; public self-registration is disabled
- Staff sign-in uses local credentials and optional passkeys only; external provider login scaffolding has been removed
- Required application roles are `Admin`, `EventManager`, `MenuManager`, and `IT`
- Authorization is policy-based, with `Admin` owning Help, User Management, Security, Publicity Editor, and Contact Editor
- `EventManager` alone can access `Event Editor`
- `MenuManager` alone can access `Menu Editor`
- `IT` alone can access the `IT / System` page
- The application seeds missing roles on startup
- A bootstrap account is created only while the site does not yet have at least one `Admin` user and one `IT` user
- The bootstrap account is seeded with both `Admin` and `IT`, is confirmed automatically for sign-in, and must change its password after first successful sign-in before broader access is allowed
- The confirmed-account requirement defaults to `false`
- The confirmed-account rule is controlled by configuration rather than the database
- `AnchorIdentity__RequireConfirmedAccount` acts as the environment-variable override
- `AnchorIdentity:RequireConfirmedAccount` in `appsettings.json` is the admin-editable fallback value
- Admins can manually confirm or unconfirm staff account access because email delivery is still implemented with the no-op sender
- Staff account confirmation is stored separately from email verification, so an approved account can stay active even if the current email address has not been re-verified yet
- Role and account-approval changes are rebuilt from the current database state on the next page refresh, so staff do not need to sign out and back in just to pick up updated access
- Admins can set a new temporary password for an existing user who forgot the current password, and that user is forced back through the password-change flow after the next successful sign-in
- Signed-in users can update their own first name, last name, and phone number from `Manage Account`, using the same validation rules as admin-managed profile updates
- `Manage Account` also shows each signed-in user a read-only list of the roles currently assigned to their account
- The app prevents removing the last `Admin` assignment or the last `IT` assignment
- An admin cannot remove the `Admin` role from their own signed-in account

## Design Direction

- Uses the printed menu as the primary visual reference for colors, tone, and structure
- Reuses the existing Anchor logo inside the shared site shell
- Applies a July 2026 menu-inspired palette with deep maritime navy, cream rules, washed gray wood surfaces, and muted blue-gray accents
- Wires the shared header theme toggle as a compact switch so reviewers can flip the live mockup between light and dark themes
- Persists the chosen light or dark theme so full-page account routes like `Account/Login` apply the saved theme immediately on load, otherwise defaulting from device theme and then time of day
- Matches the updated printed-menu typography direction with slab-serif display headings and `Barlow Condensed` for readable body and admin UI text

## Architecture

- `Anchor.Domain` contains business rules, service contracts, request and response models, and application services
- `Anchor.Infrastructure` contains `ApplicationDbContext`, EF Core entities, repository implementations, code-first migrations, and seed data
- `Anchor.Web` contains Razor components, page composition, authorization wiring, and startup configuration
- UI code resolves domain services through dependency injection and does not access `ApplicationDbContext` directly
- The solution also includes project-specific test suites for domain rules, infrastructure repositories, web rendering, and migration coverage

## Development Notes

- The development configuration now includes a LocalDB connection string so the mockup can run locally without additional secret setup
- The menu catalog now lives in the application database through the repository layer, with seed data for Lunch and Dinner food sections, first-class Dinner special items, and empty-state Breakfast and Drinks tabs that already have service hours configured
- The event foundation now lives in the application database through the repository layer, with explicit published/draft/archive state plus weekly and monthly nth-weekday recurrence rules that expand upcoming occurrences from the current request time instead of a process-start snapshot, with recurring gaps capped at one year (`52` weeks for weekly schedules and `12` months for monthly schedules)
- Date-sensitive guest logic such as menu-tab suggestions, specials, and event previews now follows the restaurant's configured local timezone instead of the host machine timezone, with the current configuration set to Central Time via `RestaurantTime:TimeZoneId`
- Homepage publicity content now lives in the application database through the repository layer, with one admin workflow for saving drafts, blank-line paragraph rendering for longer welcome copy, and a separate publish action that updates the live homepage intro
- A shared server-side GitHub issue service is now registered for future production exception reporting and technical website issue submissions
- The GitHub issue service can create repository issues and place them into the configured GitHub project status when `GitHubIssues` settings and a secure `GitHubIssues__AccessToken` are supplied
- GitHub project placement now uses the configured `GitHubIssues:ProjectOwnerType` value (`User` or `Organization`) so personal-account and organization-backed projects both resolve cleanly through the GraphQL API
- Automated production exception issue creation is gated separately by `ProductionExceptionIssues`, runs only when the ASP.NET Core environment is `Production`, and skips `localhost`, loopback, and `.local` hosts
- Production exception issue bodies include redacted request context such as route values, query values, form fields, selected headers, trace identifiers, and authenticated user roles to help recreate the failure without leaking secrets
- Repeated matching production exceptions are temporarily deduplicated in memory so GitHub does not get flooded during a burst
- The shared local development and UAT account credentials are documented in [docs/reference/uat-credentials.local.md](docs/reference/uat-credentials.local.md); if those local passwords or roles change, update that file in the same change
- The current homepage carousel uses committed placeholder photos from the venue until publicity-gallery wiring is added in a later issue
- The public events page now reads from the repository-backed event catalog, expands recurring schedules from the current request time, renders the first 90 days immediately, and can continue loading later published dates as guests scroll
- The shared header now uses a lower-profile brand row with one larger overhanging logo carrying the restaurant name through its image alt text, restored full-size desktop header labels, tighter vertical spacing in the nav/account controls, no duplicate visible site-name text, added top-page clearance so content starts below the logo, and a compact theme toggle
- Desktop header navigation stays guest-first for everyone, while signed-in staff tools move into one role-filtered `Account` dropdown instead of living in a separate inline link strip
- Mobile header navigation now opens one drawer that groups guest links first and then either `Staff Access` or authenticated account tools, with the friendly `Hi, ...` greeting moved inside those account surfaces
- Admin editor controls now share the same themed styling and normalized field sizing across light and dark modes instead of falling back to browser-default inputs
- The login page now uses the same branded themed form treatment as the rest of the site instead of the stock floating-label scaffold
- The public register entry points are disabled, and admins now create staff accounts directly from User Management with a temporary password
- Admins can update each staff member's first name, last name, and phone number from User Management without changing the sign-in email
- Signed-in users can update their own first name, last name, and phone number from Manage Account without waiting on an admin
- Admins can reset an existing staff member's password to a new temporary password from User Management when the staff member forgets it
- Passkey requests now start only after the user clicks the passkey action instead of auto-starting on login-page load
- Register, forced password change, access-denied, and register-confirmation screens now use the same branded account layout as the login page
- Public mockup pages and the admin Help page now opt out of interactive routing so header links behave like normal full page loads from login, contact, and the rest of the guest-facing site
- The shared header theme toggle and mobile menu now use browser-side JavaScript hooks instead of Blazor-only click handlers so they keep working on both static account routes and interactive admin routes
- Internal account and manage-page links now use rooted `/Account/...` routes with the same full-load navigation behavior, so moving between login, recovery, profile, and admin surfaces stays consistent from any page
- Startup bootstrap runs before the request pipeline so roles and the initial administrative account exist before the first login attempt
- Menu status labels are computed at request time from offer dates rather than stored as long-lived status flags
- Menu sections, items, special-item schedule extensions, and service windows all follow the repository pattern and are covered by unit tests plus migration-chain tests
- The Menu Editor now uses a shared time-combobox input for service hours, so staff can type shorthand like `1300` or `1pm`, pick from the reusable time list, and still keep Lunch, Dinner, Breakfast, and Drinks hours isolated cleanly across tab switches and saves
- Event-editor time entry, menu service-hour entry, and menu special start/end times are all staff-facing Central Time (`America/Chicago`) fields so what staff enter matches the restaurant's real local schedule
- The shared time-combobox now keeps cleared or partially edited hour values intact while staff are typing, and the Hours save action stays disabled until every available day has valid opening and closing times, including rejecting same-day closing times that do not come after their opening time
- The Menu Editor now separates `Food`, `Drinks`, and `Hours` into a left-rail admin console, keeps browser sections collapsed by default until staff expand them by clicking the section header, includes archive-state browsing, greyed archived records, contextual create actions, mixed parent-section content ordering for direct items plus child-section headers, a sticky focused detail panel, and built-in right-side quick guides for both the catalog workspaces and the Hours workspace that replace the old duplicated workspace intro copy
- The Hours workspace now opens with a stacked summary list so all four grouped service schedules can be compared in one scan path, and staff can click any service row to jump straight into editing that service
- The Menu Editor now uses one shared item form for both standard items and special items, with an `Is special` toggle that reveals the schedule-extension fields only when they are needed
- The Menu Editor now supports direct menu-item image uploads, staging processed `.webp` previews under `wwwroot/images/gallery/menuitems` until the related item save succeeds, rejecting raw uploads over `50 MB`, rejecting unusually large decoded image dimensions before full processing, compressing saved uploads to `5 MB` or smaller, handling common phone-camera JPG uploads through server-side normalization, automatically discarding abandoned staged uploads, cleaning up replaced local uploads after successful saves or deletes, keeping all staged and managed file operations constrained to the canonical gallery directories, rendering uploaded images as contained editor thumbnails, and keeping the manual image-path workflow behind an `Advanced image path` reveal
- Food sections now store their own default menu visibility, food items can be assigned to multiple sections, and item-level menu visibility can optionally narrow the section defaults so one item can appear in different sections for different services
- Sections can now be arranged into a single parent/child hierarchy level, so a top-level parent section can group direct items and one layer of child-section item groups while a visible child can still surface as its own root section on menus where the parent is not enabled
- Sections that already have child subsections cannot themselves be reassigned as child sections until those subsections are detached, and the editor now locks the parent picker to make that rule obvious before save time
- Menu items can now use recurring seasonal month/day windows such as October-through-April without re-entering year-specific dates, and weekly recurring specials can target multiple weekdays instead of only one
- The Menu Editor browser now keeps one item list per section and adds `All`, `Standard`, and `Specials` content filters plus `Active`, `Both`, and `Archived` archive-state browsing so staff can review the right slice of the catalog faster
- Sections now support optional guest-facing callout text, and empty sections stay hidden from guests while the editor keeps the selected empty section plus newly created empty sections visible for staff
- New section forms default to Lunch and Dinner enabled, Breakfast disabled, and Drinks disabled until staff explicitly turn those chips on for the right section family
- Menu Editor reordering now uses dedicated sort-order updates for sections and for each item-section assignment, saves mixed parent-section content reorders as one atomic operation, and still keeps special items grouped ahead of standard items on the public menu
- Menu item descriptions are optional, so simple entries like soft drinks can be saved without filler copy, and item-save errors now render inside the detail panel near the save button
- Menu section names and menu item names are now unique after trimming and case normalization, and the editor prompts staff to switch into an existing item when they blur a duplicate item name
- Existing menu items now preserve their current price-variant identities when staff add another size or serving option, so expanding an item from one price to multiple prices saves cleanly instead of colliding with stale variant rows
- Editable text-entry fields across the account pages, User Management, and Menu Editor now accept both live keystrokes and committed text-entry events, so Windows voice dictation and similar accessibility input tools behave consistently instead of only working in some forms
- Existing menu-item edits now reuse their current price-variant records instead of rebuilding them on every save, which prevents the editor from dropping the circuit on no-op or small follow-up edits
- The Drinks workspace now treats drink sections as automatically visible on the Drinks menu, hides the redundant `Drinks` toggle from the section form, and keeps new-section save disabled only until the section has a valid name
- Validation errors now highlight the affected text inputs, textareas, selects, radios, checkboxes, and shared time-combobox fields across the site so staff can immediately see which editor field needs attention
- Local Development and Debug startup now explicitly enables ASP.NET Core static web assets for direct worktree runs, so hashed CSS and JS bundles like `Anchor.Web.styles.css`, `blazor.web.js`, and component `.razor.js` files load correctly instead of leaving the site in an unstyled raw HTML state
- The shared site shell now uses fluid desktop width instead of a fixed centered column, and the public menu plus Menu Editor add larger-screen layout rules so wide displays can show more useful content at once
- Public menu hours now collapse repeated daily schedules into grouped ranges such as `Daily`, `Monday-Friday`, or `Sunday-Thursday`, and the row that includes today is highlighted instead of repeating a separate today's-hours callout
- The public menu now uses a left sidebar for service selection, keeps the active menu's weekly hours directly beneath those sidebar choices, removes the extra intro hero so guests land straight in the menu content, defaults to the service that is open now or the next one to open when guests arrive without a `tab` query string, pins the sidebar beneath the sticky site header on larger screens, opens menu categories as accordion sections with the first category expanded by default, renders visible child sections inside visible parents while promoting them to standalone sections when the parent is not enabled on that menu, preserves the editor's mixed parent-section ordering even when direct items and child-section headings share the same sort order, shows an item only once inside a rendered child-section card by preferring the deepest matching subsection assignment, evaluates upcoming dated specials by their actual scheduled occurrence dates before surfacing them as `Coming Soon`, visually indents child-section items so guests can tell they belong to a nested group, and surfaces special items in a dedicated top-level `Specials` accordion while still leaving them inside their home sections
- Menu-item thumbnails in both the editor and the public menu now open a larger in-page preview modal when clicked, so staff and guests can inspect the saved image without leaving the current page
- Non-editor elements such as navigation targets and post-navigation page headings suppress the default browser focus outline, while editor fields keep their normal editing focus behavior
- Account pages continue to use the server-routed Identity flow, so auth navigation should bypass interactive routing when needed
- Test coverage now spans domain policy/bootstrap logic, menu-domain rules, repository behavior, layout and page rendering, themed account-route integration, and the full migration chain for the Identity and menu schema
- Starter scaffold pages such as `Counter`, `Weather`, and other unused sample content have been removed from the public experience

