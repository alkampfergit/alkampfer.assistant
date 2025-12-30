---
applyTo: '.github/workflows/**'
---

If you need to check status of a running gh action use

- gh run watch
  command to monitor a specific GitHub Actions workflow run and wait for it to finish.
Command Details
This command watches the run until completion, displaying progress for all steps by default (or only relevant/failed steps with  --compact ). It refreshes every 3 seconds by default ( -i  or  --interval  to adjust).
Key Options
	•	 --exit-status : Exits with non-zero status if the run fails, useful in scripts.
	•	 -R, --repo <HOST/OWNER/REPO> : Targets a specific repository.
	•	Note: Requires classic PATs (not fine-grained) due to  checks:read  permission limits.
Usage Example
First, get the run ID with  gh run list , then:
 gh run watch <run-id> --exit-status 
This blocks until done and propagates the failure exit code.