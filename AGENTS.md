# Repository Workflow

## Shared Parent Guidance

- Default to the shared agent file at `C:\Github\AGENTS.md`.
- Apply this repository file together with the shared parent file.
- If this repository file conflicts with the shared parent file, this repository file overrides it for work in this repository.

## Repository-Specific Workflow

- Treat `main` and `dev` as protected branches once they exist.
- Never commit directly to `main`.
- All changes must reach `main` through a pull request.
- Non-code-related changes may be committed directly to `dev`.
- All new work branches and worktrees must be created from `origin/dev`, not local `dev`, so they start from the latest remote unreleased changes.
- After every code-related change, commit the change and push the branch so code work is not left only in the local repository.
- Every pull request from `dev` into `main` must include release notes covering all changes since the previous merge from `dev` into `main`.

# Documentation Maintenance

- Whenever changes are made to website functionality or backend application behavior, update `README.md`.
- `README.md` updates must remove information that no longer applies and add information for new features, backend behavior changes, or other relevant application changes.

# Local Development Credential Reference

- The tracked reference file `docs/reference/uat-credentials.local.md` is the shared source of truth for Codex-created local development and UAT account credentials.
- If Codex creates one of those documented accounts, resets or changes the password for one of those documented accounts, or changes the role assignments for one of those documented accounts, Codex must update `docs/reference/uat-credentials.local.md` in the same change so later conversations have the current username, password, and role information.
- Codex must not leave the only copy of those credentials in a worktree-only file, an ignored local file, or any other ephemeral note that would disappear when a worktree is removed.
- If a private machine-specific override note is ever needed, use `docs/reference/uat-credentials.private.local.md` for that purpose and keep `docs/reference/uat-credentials.local.md` updated with the shared current values that future Codex conversations need.

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

# Pull Requests And Testing

- In addition to the shared branch-level coverage requirements, all new and changed code in this repository must be covered by unit tests before it is merged into `dev`.
- Every project within the solution must have a corresponding unit test project.
- Projects that own database migrations must also have a separate migration-focused test project.
- Whenever code changes are made, unit tests must be written for those changes.
- Code-related pull requests must include the unit tests required to validate the new or changed behavior.
- Unit tests must include both positive and negative test cases.
- Unit tests must cover database migrations.
- When model changes require a database migration, the related tests must validate migrations from the initial database creation through the current migration.
- Migration-related tests must verify data transformations across the full migration chain to ensure the database evolves correctly.
- Unit tests required for a change must pass before that change is merged into `main`.

# Code Review Workflow

- Use the shared `In Review` automation and reviewer workflow from `C:\Github\AGENTS.md`.
- Pull requests from a feature branch into `dev` must be created as draft pull requests first.
- Do not report code implementation as complete until the current feature branch has a draft pull request into `dev`; a pushed branch without a pull request is still incomplete work.
- Unless the user explicitly wants the pull request to remain draft or there is an active blocker, mark the pull request ready for review as soon as implementation, required tests, and required documentation updates are complete.
- When implementation and required tests are complete, mark the pull request ready for review and move the related issue to `In Review`.
- Do not report the ticket workflow as complete while a pull request that already meets the ready-for-review criteria is still left in draft state.
- In addition to the shared automated review flow, record three pull request comments for scope validation, code quality review, and performance review.
- Each review-stage comment must document whether that stage passed or whether changes are required.
- Review-result comments must be recorded on the pull request, not on the issue.
- Complete the merge from the feature branch into `dev` only after the shared automated review workflow and the three review-stage comments are complete.

# Release Notes

- Every pull request from `dev` into `main` must include release notes covering all changes since the previous merge from `dev` into `main`.
- Release notes must include a short summary of the release.
- Release notes must include an itemized list of changes made in the release.
- When applicable, release notes should also identify bugs fixed and list the issues included in the release.
- Release notes should focus on application features, fixes, and code changes, and do not need to include repo-only or other non-code-related changes.
- When the pull request from `dev` into `main` is merged, move each included issue to `Released` and then close the issue.

# GitHub Comment Signature

- GitHub issue comments and pull request comments written by Codex must end with a signature line: `~ Codex`
- This signature is required because Codex and the user currently share the same GitHub account and comments need a clear way to distinguish Codex-authored messages from user-authored messages.
