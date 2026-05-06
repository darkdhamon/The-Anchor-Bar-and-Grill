# Repository Workflow

- Treat `main` and `dev` as protected branches once they exist.
- Never commit directly to `main`.
- All changes must reach `main` through a pull request.
- Non-code-related changes may be committed directly to `dev`.
- Code-related changes must be made in a feature branch first.
- Code-related changes must be brought into `dev` through a pull request, not through a direct merge outside the pull request workflow.
- Every pull request from `dev` into `main` must include release notes covering all changes since the previous merge from `dev` into `main`.

# Change Classification

- Non-code-related changes include updates to `AGENTS.md`, documentation files, and similar repository guidance or reference content.
- Configuration files are considered code-related changes.
- Continuous integration files are repository-related changes, not code-related changes, because they are part of repository setup and do not affect application runtime behavior directly.
- Deployment scripts are not code-related changes.
- Code-related changes include anything inside the C# codebase, anything that makes the web app work, and changes to dependencies used by that application.
- If a work item includes both code-related and non-code-related changes, treat the entire work item as code-related and follow the stricter code-change workflow.

# Documentation Maintenance

- Whenever changes are made to website functionality or backend application behavior, update `README.md`.
- `README.md` updates must remove information that no longer applies and add information for new features, backend behavior changes, or other relevant application changes.

# User Experience And Help Content

- The website should be made as user-friendly as possible.
- Do not assume website users, especially admin users, already know how to operate the site.
- Admin-facing features must include on-page help documentation that explains how the page works and what the user should do on that page.
- When new admin features are added, or existing admin features are changed, the related help content must be added or updated as part of the same work.
- The website help section must be kept up to date whenever user-visible or admin-visible features are implemented or changed.
- The website help section should only be visible to logged-in users.
- Guests should not see the help section because it documents admin functionality rather than public website usage.

# Code Quality And Architecture

- Do not create monolithic code files that are difficult for developers to navigate or understand.
- Keep code organized into focused, maintainable files and classes with clear responsibilities.
- Follow established best practices and pay attention to SOLID principles.
- Structure the solution as multiple projects with clear separation of concerns.
- `Anchor.Web` should primarily contain UI-focused code.
- A domain project should contain business logic.
- The domain project should create and expose services that `Anchor.Web` consumes through dependency injection.
- Additional projects may be introduced when they improve separation of concerns and maintainability.

# Styling

- CSS changes should be implemented globally whenever possible.
- Avoid repeating CSS separately for individual pages when the styling can be shared.
- Prefer shared global styles so UI changes can be applied consistently across the website.

# Database And Data Access

- Use SQL Server as the application database platform.
- Use code-first migrations for database schema changes.
- Use a repository pattern for database access.
- Do not access the database context directly from application code outside the repository layer.
- Database access must be efficient and should retrieve only the data needed for the current operation.
- Do not load entire tables or large data sets when only a small subset of data is required.
- Filter data at the database level as much as possible.
- Use tables, views, or other appropriate database objects when needed to support efficient querying.
- Pay close attention to Entity Framework query behavior and avoid inefficient data access patterns.
- Ensure Entity Framework usage remains efficient whenever the application accesses the database.

# Logging

- Implement application logging in a way that is easy to use from the codebase.
- Do not depend on third-party hosted logging infrastructure or external systems such as Kafka for core application logging.
- Prefer logging to the application database.
- If the database is unavailable, fall back to logging to the local file system within the application's local directory.
- Logging levels may be chosen based on implementation needs, but the logging approach should remain practical and maintainable.
- Logs should be visible to admin users through the application.

# Collaboration Workflow

- When discussing an issue or possible feature, do not begin coding immediately.
- Start by planning the work together before implementation begins.
- Planning should include implementation approach, design choices, and other relevant decisions needed before code is written.
- If a conversation starts with an issue number, analyze the task, create the feature branch immediately, and switch to that branch before continuing.
- When creating a feature branch for an issue-driven conversation, base it on `dev` if `dev` exists; otherwise base it on `main`.
- Only begin code implementation when the user explicitly says to implement the code.
- Before code work begins, confirm whether an issue already exists for the work.
- If no issue exists, create one before starting code implementation.
- After planning and research are complete for an issue, update the issue description so it matches the current agreed scope and implementation plan before code work begins.
- Once the issue scope, implementation plan, and description are finalized, move the issue from `Backlog` to `Ready`.
- After the issue is confirmed or created, create the feature branch if it has not already been created.
- When code implementation begins, move the issue from `Ready` to `In Progress`.
- Once planning is complete, the issue is ready, and the feature branch exists, wait for explicit user direction before starting code changes.

# Pull Requests And Testing

- The primary measurable pull request requirement is unit test coverage and passing unit tests.
- Every project within a solution must have a corresponding unit test project.
- Projects that own database migrations must also have a separate migration-focused test project.
- Whenever code changes are made, unit tests must be written for those changes.
- All new code must be fully covered by unit tests before it is merged into `dev`.
- Code-related pull requests must include the unit tests required to validate the new or changed behavior.
- Unit tests must cover all changed code and all newly written code.
- No explicit numeric coverage threshold is defined yet; the current requirement is that all new and changed code must be covered.
- Unit tests must include both positive and negative test cases.
- Unit tests must cover database migrations.
- When model changes require a database migration, the related tests must validate migrations from the initial database creation through the current migration.
- Migration-related tests must verify data transformations across the full migration chain to ensure the database evolves correctly.
- Unit tests required for a change must pass before that change is merged into `main`.

# Code Review Workflow

- Pull requests from a feature branch into `dev` must go through code review before merging.
- Do not rely on GitHub branch protection or forced reviewer settings for this process because the repository currently operates with a single user account.
- Track code review progress manually.
- When creating a pull request from a feature branch into `dev`, create it as a draft pull request first.
- When the pull request from the feature branch into `dev` is created, move the related issue from `In Progress` to `In Review`.
- The first review stage is scope validation.
- Scope validation confirms that the code does what the issue requires, covers the full intended scope of the issue, and includes the required unit test coverage.
- After the first review stage, add a pull request comment documenting the review results, including whether it passed or whether changes are required.
- The second review stage is code quality review.
- Code quality review confirms that the implementation follows project standards, including SOLID principles and general maintainability expectations.
- After the second review stage, add a pull request comment documenting the review results, including whether it passed or whether changes are required.
- The third review stage is performance review.
- Performance review confirms that the implementation operates as efficiently as possible.
- After the third review stage, add a pull request comment documenting the review results, including whether it passed or whether changes are required.
- Review-result comments must be recorded on the pull request, not on the issue.
- After all three review stages are completed, complete the merge from the feature branch into `dev`.
- After the feature-branch pull request is merged into `dev`, move the related issue to `In Dev Branch`.

# Issue Requirement

- All code-related changes must be attached to an issue.
- New issues should be added to the project board in the `Backlog` column when they are created.
- If code work does not already have an issue, create or confirm an issue before starting implementation.
- Before creating a new issue, first check for an existing issue that matches the requested change.
- If a possible matching issue already exists, confirm with the user whether that existing issue should be used or whether a new issue should be created.
- Pull requests that close or resolve issues should use GitHub closing keywords so the issue is closed automatically when the related change reaches `main`.

# Branch Naming

- Branch names must be based on the issue number and the issue title.
- Codex-created code branches must use the format `codex/<issue-number>-<issue-title>`.
- The issue title portion of the branch name should be normalized as a lowercase, hyphen-separated slug.

# Branch Terminology

- References to `main` may also be expressed as `master`, `prod`, or `production`.
- References to `dev` may also be expressed as `dev` or `development`.

# Release Notes

- Every pull request from `dev` into `main` must include release notes covering all changes since the previous merge from `dev` into `main`.
- When a pull request from `dev` into `main` is created, move every issue included in that pull request from `In Dev Branch` to `In Plan Release`.
- When the pull request from `dev` into `main` is merged, move each included issue to `Done` and then close the issue.
- Release notes must include a short summary of the release.
- Release notes must include an itemized list of changes made in the release.
- When applicable, release notes should also identify bugs fixed and list the issues included in the release.
- Release notes should focus on application features, fixes, and code changes, and do not need to include repo-only or other non-code-related changes.

# Codex Message Formats

- This section mirrors the current Codex application defaults so the repository retains a portable copy of them.

## GitHub Comment Signature

- GitHub issue comments and pull request comments written by Codex must end with a signature line: `- Codex`
- This signature is required because Codex and the user currently share the same GitHub account and comments need a clear way to distinguish Codex-authored messages from user-authored messages

## Commit Message Format

```text
Codex - Issue #{issue number} - {Title}

{Summary of changes}
```

## Pull Request Format

```text
Codex - {pr title}

{List of affected issues}

{Summary of changes}
```
