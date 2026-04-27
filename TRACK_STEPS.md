Before proceeding to the next step, you must first analyze everything that has already been built.

Open the `completed-steps/` folder in the project root. It contains one subfolder per completed step (step1/, step2/, step3/, step4/). Inside each subfolder there is a `task.md` and a `walkthrough.md` written by you after completing that step.

Read every single file in this order:
- completed-steps/step1/task.md
- completed-steps/step1/walkthrough.md
- completed-steps/step2/task.md
- completed-steps/step2/walkthrough.md
- completed-steps/step3/task.md
- completed-steps/step3/walkthrough.md
- completed-steps/step4/task.md
- completed-steps/step4/walkthrough.md
- completed-steps/step5/task.md
- completed-steps/step5/walkthrough.md
- completed-steps/step6/task.md
- completed-steps/step6/walkthrough.md
- completed-steps/step7/task.md
- completed-steps/step7/walkthrough.md
- completed-steps/step8/task.md
- completed-steps/step8/walkthrough.md
- completed-steps/step9/task.md
- completed-steps/step9/walkthrough.md
- completed-steps/step10/task.md
- completed-steps/step10/walkthrough.md
- completed-steps/step11/task.md
- completed-steps/step11/walkthrough.md
- completed-steps/step12/task.md
- completed-steps/step12/walkthrough.md
- completed-steps/step12-verification/task.md
- completed-steps/step12-verification/walkthrough.md
- completed-steps/step13A/walkthrough.md
- completed-steps/step13B/walkthrough.md
- completed-steps/step13C/walkthrough.md
- completed-steps/step13G/task.md
- completed-steps/step13G/walkthrough.md

After reading all of them, tell me:
1. What was built in each step — a 2-3 sentence summary per step
2. Any decisions or deviations from AGENT_START_HERE.md that were made (e.g. different package versions, renamed files, structural changes, anything that differs from the plan)
3. Any known issues, TODOs, or incomplete items flagged in the walkthroughs
4. The exact current state of the project — what exists, what port each service runs on, what databases have been created

This analysis is mandatory because everything you build next depends on what already exists. If you misunderstand the current state, you will create conflicts, duplicate code, or break working functionality.

Once you have given me that summary, tell me what Step 8 is and ask for confirmation before starting it.

Also: going forward, after completing every future step, create a completed-steps/stepN/ folder and write task.md (what was supposed to be done) and walkthrough.md (what you actually did, any deviations, any issues found) so the record stays current.