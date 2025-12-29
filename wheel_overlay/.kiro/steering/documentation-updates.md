---
inclusion: always
---

# Documentation Update Guidelines

## When to Update Documentation

You MUST update documentation files in the following situations:

1. **After Completing a Spec**: When all tasks in a spec are marked as complete
2. **Before Pushing to Remote**: When preparing to push changes to a branch
3. **After Adding New Features**: When new functionality is implemented
4. **After Bug Fixes**: When significant bugs are fixed that affect usage
5. **After Version Changes**: When the version number is updated

### Files to Update

When making changes, update these files as appropriate:

- **README.md** - User-facing documentation and getting started guide
- **CHANGELOG.md** - Historical record of all changes (see changelog-maintenance.md steering)
- Both files serve different purposes and should be kept in sync

## What to Update in README.md

### Version Information
- Update the version number in the "Version History" section
- Add a new entry describing the changes in the current version
- Move the previous "Current" version to the history list

### Features Section
- Add new features to the appropriate version section (e.g., "v0.4.0 New Features")
- Use clear, user-friendly language
- Include bullet points for each major feature

### Getting Started / Usage
- Update instructions if new features change how users interact with the application
- Add new sections for significant new functionality
- Update screenshots or add new ones if UI has changed

### Troubleshooting
- Add common issues that users might encounter with new features
- Include solutions or workarounds
- Update existing troubleshooting steps if they've changed

### Development Section
- Update build instructions if dependencies or requirements change
- Add new development-related features (like test mode)
- Update testing instructions if test framework changes

## README.md Structure

The README should maintain this structure:

1. **Title and Description**: Brief overview of the application
2. **Features**: Organized by version, newest first
3. **Installation**: How to install the application
4. **Getting Started**: First-time setup guide
5. **Usage**: Detailed usage instructions
6. **Troubleshooting**: Common issues and solutions
7. **Development**: Building and testing instructions
8. **Contributing**: How to contribute
9. **License**: License information
10. **Support**: Where to get help
11. **Version History**: Changelog with version numbers and dates

## Documentation Quality Standards

### Clarity
- Use clear, concise language
- Avoid technical jargon when possible
- Explain technical terms when necessary

### Completeness
- Cover all major features
- Include examples where helpful
- Provide troubleshooting for common issues

### Accuracy
- Ensure all instructions are correct and up-to-date
- Test instructions before documenting them
- Update outdated information immediately

### User-Focused
- Write from the user's perspective
- Focus on "how to" rather than "what is"
- Include practical examples and use cases

## Checklist Before Pushing

Before pushing changes to a branch, verify:

- [ ] README.md version number matches the project version
- [ ] New features are documented in the Features section
- [ ] Usage instructions are updated if needed
- [ ] Troubleshooting section includes any new common issues
- [ ] Version History section has an entry for the current version
- [ ] All code examples and commands are tested and accurate
- [ ] Links to external resources are valid
- [ ] Formatting is consistent throughout the document
- [ ] CHANGELOG.md is updated with all changes (see changelog-maintenance.md)
- [ ] CHANGELOG version and date are finalized before merging
- [ ] Both README and CHANGELOG tell a consistent story

## Example Version History Entry

```markdown
### v0.4.0 (2024-01-15)
- Added About Wheel Overlay dialog with version info and GitHub link
- Smart text condensing automatically hides empty positions
- Visual flash animation feedback for empty position selection
- Enhanced Single layout to display last populated position
- Test mode for development without physical hardware
- Comprehensive integration test suite with property-based testing
```

## Automation Reminder

When you complete a spec or prepare to push:
1. Review the changes made in the current branch
2. Update README.md with all relevant changes
3. Commit the README.md update with a clear message
4. Include the README update in the same push as the feature changes

## Notes

- Keep the README focused on user-facing features and usage
- Technical implementation details belong in code comments or separate developer documentation
- Screenshots and GIFs are valuable - update them when UI changes significantly
- Consider the README as the first impression for new users - make it welcoming and helpful
