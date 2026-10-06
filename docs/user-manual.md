# DUT Gym e-Application User Manual

## 1. Purpose of the System

The DUT Gym e-Application helps students, staff, trainers, and administrators manage gym membership, attendance, fitness goals, nutrition planning, personal training, class bookings, achievements, and reporting in one web application.

The system is designed for a university gym environment where students apply for membership, staff manage check-ins, trainers support students, and administrators oversee approvals, payments, equipment, reports, and gym operations.

## 2. User Roles

### Student

Students use the system to:

- Register with a DUT student email address.
- Apply for a gym membership plan.
- Track membership status.
- Check gym capacity before visiting.
- Set and track fitness goals.
- View attendance streaks.
- Build weekly meal plans.
- Browse and book sessions.
- Request a personal trainer.
- View XP, badges, rewards, and leaderboards.
- Unlock, view, and download certificates.

### Staff

Staff users are operational gym employees. They use the system to:

- Search for active student members.
- Check students into the gym.
- Check students out of the gym.
- View live capacity.
- Monitor current occupancy.

Staff accounts require admin approval before access is granted.

### Trainer

Trainers use the system to:

- Apply for trainer access.
- Wait for admin approval.
- View incoming student trainer requests.
- Accept or reject assigned students.
- Upload workout plans.
- Schedule trainer sessions.
- View their assigned students.

Trainer accounts require admin approval before access is granted.

### Admin

Admins manage the full gym operation. They can:

- Approve or reject staff applications.
- Approve or reject trainer applications.
- Approve, reject, edit, deactivate, or reactivate memberships.
- Confirm cash payments.
- Manage membership plans.
- Manage venues.
- Manage sessions.
- Manage capacity slots.
- Manage equipment.
- Create and manage challenges.
- View attendance and usage reports.
- View dashboard analytics.

## 3. Account Registration and Login

### Student Registration

Students register using a DUT student email address ending in:

```text
@dut4life.ac.za
```

When a student signs up with this email domain, the system automatically assigns the **Student** role.

After successful registration, the student is taken into onboarding and can begin setting a fitness goal.

### Staff Registration

Staff register using a DUT staff email address ending in:

```text
@dut.ac.za
```

The system automatically assigns the **Staff** role. Staff accounts start as **Pending** until approved by an admin.

### Trainer Application

Trainers apply through the trainer application flow. Trainer accounts also start as **Pending** until an admin approves them.

### Admin Access

Admin accounts are not created through public signup. Admin users are promoted manually in the database for security.

## 4. Student Dashboard

After login, a student lands on the Student Dashboard. This page gives a quick overview of:

- Full name.
- Student number.
- Email address.
- Membership plan.
- Membership status.
- Gym capacity.
- XP and level.
- Attendance streak if available.

The dashboard also provides navigation to:

- Browse Plans.
- Apply for Membership.
- View Membership Status.
- My Fitness Goal.
- My Trainer & Plans.
- Class Schedule.
- Meal Planner.
- Achievements.

## 5. Membership Workflow

### Applying for Membership

A student selects a membership plan and completes the membership application form. The form captures important details such as campus, emergency contact, medical consent, and optional personal trainer selection.

### Payment

After applying, the student proceeds to payment. The system supports several payment method states:

- Card, EFT, and mobile money are treated as paid immediately in the system.
- Cash starts as pending until an admin confirms that money was received.

### Admin Review

Admins review pending membership applications. They can:

- Approve the membership.
- Reject the membership.
- Confirm cash payments.
- Edit membership details.
- Deactivate active memberships.
- Reactivate deactivated memberships.

Once approved and paid, the student becomes an active gym member.

## 6. Staff Check-In Workflow

Staff use the check-in area to search for students with active memberships.

The check-in process is:

1. Staff opens the check-in page.
2. Staff searches by student number or email, or selects from the active member list.
3. Staff opens the student detail page.
4. If the student has an active paid membership, staff checks them in.
5. When the student leaves, staff checks them out.

Check-ins are important because they drive:

- Attendance reports.
- Live gym capacity.
- Student streaks.
- Certificate eligibility.
- XP and gamification progress.

## 7. Fitness Goals

Students can set a personal fitness goal. The system supports goal types such as:

- Weight loss.
- Muscle gain.
- General fitness.
- Tone.
- Endurance.

When setting a goal, students can provide:

- Description.
- Starting weight.
- Target weight.
- Activity level.

The student can later log progress, but progress logging requires that they have checked into the gym that day. This prevents students from logging gym progress without attendance.

## 8. Certificates

### What the Certificate Is

The system awards a **Certificate of Consistency** to students who attend the gym regularly within the current month.

The certificate is not manually issued by staff. It is automatically unlocked by the system based on real attendance check-ins.

### Requirement

To unlock the certificate, the student must complete:

```text
10 gym check-ins in the current calendar month
```

For example, in October, the system counts check-ins from 1 October to 31 October. When the student reaches 10 visits, the certificate becomes available.

### Where Students See Certificate Progress

Students open:

```text
My Fitness Goal
```

On that page, they see a **My Certificate** section. This section shows:

- The requirement.
- The number of visits completed this month.
- The number of visits still needed.
- A progress bar.
- Status: In Progress or Unlocked.
- A certificate timeline.

### Certificate Timeline

The timeline helps students understand exactly what is left before they qualify:

1. **Month started**: attendance tracking is active.
2. **Reach halfway target**: progress milestone toward the certificate.
3. **Reach 10 visits**: certificate requirement is complete.
4. **View or download certificate**: certificate is available once unlocked.

### Viewing and Downloading

Before qualification, the View and Download buttons are disabled.

Once the student reaches 10 visits:

- **View My Certificate** becomes active.
- **Download Certificate** becomes active.
- The certificate can also be printed or saved as a PDF from the browser.

The downloaded certificate includes:

- Student name.
- Month.
- Number of visits completed.
- Issue date.
- DUT Gym branding.

## 9. Meal Planner

### Purpose

The Meal Planner helps students plan affordable meals for the week. It supports both suggestions and manual planning.

### Daily Nutrition Target

If the student has set a fitness goal, the Meal Planner estimates:

- Target calories.
- Protein.
- Carbs.
- Fat.

This is based on the student’s goal, starting weight, gender, and activity level. It is a guide only, not medical advice.

### Healthy Eating Tips

The system shows goal-based nutrition tips. For example, a student focused on muscle gain may see tips about protein intake, while a student focused on weight loss may see tips about portion control and high-fibre meals.

### Searching for Meal Suggestions

Students can enter ingredients they already have, separated by commas.

Example:

```text
rice, eggs, onion, tomato
```

The system compares these ingredients with its built-in affordable recipe list and returns meals the student can make.

Each suggestion shows:

- Meal name.
- Estimated cost.
- Ingredients.
- Calories.
- Protein, carbs, and fat.
- Simple preparation instructions.

### Adding a Suggested Meal to the Weekly Plan

After searching, each meal suggestion has an **Add to My Week** option.

The student chooses:

- Day of the week.
- Meal slot: Breakfast, Lunch, Dinner, or Snack.

When they add the meal, it is saved into their weekly meal plan.

If they add another meal to the same day and slot, the system replaces the old meal in that slot.

### Adding Meals Manually

Students can also add meals manually without using suggestions.

They can enter:

- Day.
- Meal slot.
- Meal name.
- Notes.
- Calories.
- Protein.
- Carbs.
- Fat.
- Estimated cost.

This is useful when the student already knows what they want to eat, or when the meal is not in the suggestion list.

### Editing the Weekly Meal Plan

Saved meals appear under **My Weekly Meal Plan**.

Students can edit:

- Meal name.
- Notes.
- Calories.
- Protein.
- Carbs.
- Fat.
- Estimated cost.

After editing, they click **Save Changes**.

### Removing Meals

Students can remove a saved meal from the weekly plan by clicking **Remove**.

### Weekly Meal-Prep Menu

The system also shows a suggested weekly meal-prep menu. This menu is a guide and changes over time, but the student’s own saved meal plan is the main editable plan.

## 10. Sessions and Bookings

Students can browse scheduled gym sessions and book available classes.

Admins manage the session catalogue, including:

- Session name.
- Category.
- Venue.
- Instructor.
- Start and end time.
- Capacity.

Students can view their bookings from the sessions area.

## 11. Personal Trainer Workflow

Students can browse approved trainers and send trainer requests.

Trainer request flow:

1. Student selects a trainer.
2. Student sends a request.
3. Trainer reviews incoming requests.
4. Trainer accepts or rejects.
5. If accepted, the trainer can manage that student.

Trainers can upload workout plans and schedule sessions.

Students can view trainer details, uploaded plans, and scheduled personal trainer sessions from **My Trainer & Plans**.

## 12. Achievements, XP, Badges, and Rewards

The system includes gamification to encourage regular gym engagement.

Students can earn XP from actions such as:

- Checking in.
- Setting a goal.
- Logging progress.
- Booking classes.
- Completing streak milestones.
- Completing challenges.

XP contributes to levels:

- Beginner.
- Regular.
- Beast Mode.

Badges are awarded for achievements such as:

- Early check-ins.
- Weekly attendance consistency.
- Class participation.

Rewards unlock when the student reaches XP thresholds.

Students view these from:

```text
Achievements
```

## 13. Friends and Leaderboards

Students can add friends and compare XP progress.

Leaderboards can show:

- Weekly XP.
- Monthly XP.
- Friends leaderboard.

This gives students a social motivation layer and helps demonstrate engagement across the gym community.

## 14. Admin Dashboard and Reports

The Admin Dashboard gives an overview of gym operations.

Admins can see:

- Total members.
- New members this month.
- Active memberships.
- Revenue this month.
- Current gym occupancy.
- Members by campus.
- Membership status split.
- Monthly attendance trends.

The reports page shows:

- Total check-ins.
- Average time spent per visit.
- Peak hours.
- Peak days.
- Monthly attendance.
- Member of the Month leaderboard.

These analytics are powered by real data such as memberships, payments, and check-ins.

## 15. Equipment Management

Admins and staff can manage gym equipment records.

Equipment records can include:

- Name.
- Status.
- Severity.
- Venue.
- Serial number.
- QR code.
- Image.
- Purchase details.

Admins have additional control over inactive equipment.

## 16. Capacity Management

The system tracks live gym capacity based on open check-ins.

Capacity supports:

- Student visibility of current gym busyness.
- Staff monitoring.
- Admin management of capacity slots.
- Waitlist behavior for capacity slots.

Students can use this to decide the best time to visit the gym.

## 17. Demo and Presentation Guide

### Suggested Presentation Flow

1. Start at the login page.
2. Explain role-based access.
3. Log in as a student.
4. Show the Student Dashboard.
5. Open Membership Status and explain approval/payment flow.
6. Open My Fitness Goal.
7. Explain attendance streaks and certificate progress.
8. Show the certificate timeline.
9. Open Meal Planner.
10. Search ingredients and add a suggested meal to the week.
11. Add a manual meal.
12. Edit and remove a meal.
13. Open Achievements to show XP, badges, and rewards.
14. Log in as admin.
15. Show pending approvals and reports.
16. Explain how admin analytics are based on operational data.

### Key Points to Emphasise

- The system is role-based.
- Students cannot approve themselves.
- Staff check-ins drive attendance, capacity, streaks, certificates, and reports.
- Certificates are earned from actual monthly visits.
- Meal planning is editable, not just suggestions.
- Admin analytics become more useful as the database fills with memberships, payments, and check-ins.
- The application supports both daily operations and student engagement.

## 18. Troubleshooting

### Certificate Buttons Are Disabled

The student has not reached 10 check-ins in the current month yet.

### Certificate Does Not Appear After Check-In

Refresh the My Fitness Goal page. The certificate status is calculated from check-in records.

### Meal Suggestion Search Returns Nothing

Add more ingredients or use common ingredients such as:

```text
rice, eggs, onion, tomato, chicken, oats, bread
```

### Staff Cannot Access Dashboard

The staff account is probably still pending admin approval.

### Trainer Cannot Access Dashboard

The trainer application is probably still pending admin approval.

### Student Cannot Check In

The student must have an active, paid-up membership.

## 19. Summary

The DUT Gym e-Application supports the full gym journey:

- Register.
- Apply for membership.
- Pay.
- Get approved.
- Check in.
- Track goals.
- Plan meals.
- Earn badges and certificates.
- Book sessions.
- Work with trainers.
- Support admin reporting.

It is designed to show both operational control for gym staff/admins and engagement features for students.
