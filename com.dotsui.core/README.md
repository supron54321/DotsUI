## IMPORTANT NOTE: This project is not ready for production. Please use it only for learning purposes. Until v1.0.0, core will rapidly evolve and breaking changes may occur ##

# Dots UI System #

Dots UI System is an Open Source MIT Licensed UI system for the new [Data Oriented Technology Stack](https://unity.com/dots).

# High level goals #

## Performance ##

This is our highest priority. We are aiming for the fastest Unity UI solution. If you are looking for the feature-rich UI system, you should find another UI framework.

## Independent rendering and input system ##

Current implementation creates unity Mesh and CommandBuffer objects per canvas, but we are working on modular render system. In the future we will support both UnityEngine and pure DOTS/Tiny renderers.

## Modular design ##

Project is split into multiple packages: Core, Hybrid renderer/input, and Tiny renderer/input (Soon).

# Documentation #

[Table of contents](Documentation~/TableOfContents.md)

# Contribution #

Since Core is not ready yet, I do not accept pull requests for new features. If you have any suggestions, please create issue. If you really want to help, please test this package and report all bugs (check [KnownIssues](Documentation~/KnownIssues.md) before posting a new issue).

# FAQ #

Q: Is it production ready?
A: No. It's still far away form this. We are working on the core design. Stabilization is not our highest priority yet.

Q: Can I contribute?
A: Yes. No pull request yet. However you can test and post issues.

Q: Can I use it in commercial project?
A: Yes! It's MIT licensed. You can use it, sell it or do whatever you want.