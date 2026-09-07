# Sprint 2 Backlog — from Sprint 1 Feedback

Source: sprint 1 feedback.docx

## Bugs (fix first)

- [x] **Progress logging accepts null values.** Logging progress with empty/null fields still reports "progress logged" — add validation to reject incomplete submissions.
- [x] **Progress can be logged without a gym check-in.** A student should only be able to log fitness progress on a day they were checked in at the gym.

## Validation / UX gaps

- [x] No feedback after logging progress (e.g. weight loss). Add a response reflecting the update, e.g. "You're 3kg away from your goal!"

## UI Polish

- [x] Overall app UI doesn't read as a gym system — add imagery/branding, improve visual appeal. (Bootstrap Icons + stat tiles added; no new photography/illustrations since none were supplied.)
- [x] Student dashboard is dull/unstructured — redesign layout, consider gamification elements. (icons, streak badge, progress bar)
- [x] Student dashboard must show the student's current membership plan type.
- [x] Desk staff dashboard — general look-and-feel pass. (stat tiles: check-ins today, occupancy, equipment faults)

## Fitness Goals — feature expansion

- [x] Expand visit tracking beyond monthly visit count:
  - [x] Time spent at the gym per visit
  - [x] Days attended per week
  - [x] Average time spent overall
  - [x] Motivational/incentive-driven elements to encourage continued progress
- [x] Daily schedule view for students (e.g. aerobics, Zumba classes)
- [x] Personal trainers can create fitness plans for members; members can view their assigned plan (this already existed via Trainers/UploadPlan + MyTrainer — now surfaced on the dashboard)
- [x] Diet/nutrition features:
  - [x] Chatbot/tool that suggests affordable meals from ingredients the student logs (rule-based, no external AI)
  - [x] Weekly meal-prep menu suggestions

## Desk Staff — feature expansion

- [x] Equipment maintenance logging:
  - [x] Mark equipment as faulty/inactive
  - [x] Request maintenance
  - [x] Rank severity/criticality of the damage

## Live Gym Capacity & Admin Reports

- [x] Reports currently too sparse — expand with:
  - [x] Peak hours broken down by day of week
  - [x] Average time spent by students
  - [x] Leaderboards to drive student incentive/engagement
