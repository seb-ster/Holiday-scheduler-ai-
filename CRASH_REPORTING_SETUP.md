# Automatic Crash Reporting & Auto-Release Setup

This guide explains how to set up automatic crash reporting so you can fix bugs and release updates with minimal user interaction.

## Overview

```
Users' App (crashes)
    ↓ (automatic, background)
Your Backend Server (collects reports)
    ↓ (you review)
Send crash data to me
    ↓ (I analyze)
I suggest fix + create PR
    ↓ (GitHub Actions builds)
App auto-updates users next startup
```

## Architecture

- **App**: Sends crashes automatically to backend (zero user annoyance)
- **Backend**: Simple Node.js server that stores crash reports
- **GitHub Actions**: Auto-builds and releases when code is pushed to main
- **Users**: Auto-updates happen silently next time they launch the app

## Deployment Steps (15 minutes)

### Step 1: Deploy Backend (~5 minutes)

**Choose one option:**

#### Option A: Railway (Recommended)
1. Go to https://railway.app and sign up (free)
2. Click "New Project" → "Deploy from GitHub repo"
3. Select your `holiday-roster-ai` repo
4. Railway auto-detects Node.js
5. URL generated automatically (e.g., `https://holiday-scheduler-xy.railway.app`)
6. **Copy this URL** (you'll need it next)

#### Option B: Render
1. Go to https://render.com and sign up
2. Create new "Web Service" → Connect GitHub
3. Set "Root Directory" to `backend`
4. Deploy
5. **Copy deployment URL**

### Step 2: Configure App (~2 minutes)

Set environment variable before launching app:

```bash
export HOLIDAY_SUPPORT_ENDPOINT=https://your-deployed-backend-url.com

# Then launch:
/Applications/Holiday\ Scheduler\ Demonstrator.app/Contents/MacOS/Holiday\ Scheduler\ Demonstrator
```

Or use the helper script:

```bash
./launch_with_backend.sh https://your-deployed-backend-url.com
```

### Step 3: Test Backend (~3 minutes)

```bash
# Check if backend is working
curl https://your-backend-url.com/health
# Response: {"status":"ok","timestamp":"2026-03-11T10:30:00.000Z"}

# View crash statistics
curl https://your-backend-url.com/api/stats
# Response: {"totalCrashes":0,"totalFeedback":0,"crashTypes":{},...}
```

### Step 4: Release to Users (~3 minutes)

Build and release new version:

```bash
cd gui/avalonia
dotnet build -c Release
./release_all.sh mac
cd ..
git add -A
git commit -m "Fix: [description of fix]"
git push origin main
```

GitHub Actions automatically:
1. ✅ Builds the app
2. ✅ Creates a GitHub release with `.zip`
3. ✅ Users get auto-update next launch

## Workflow: Bug → Fix → Release

### When Users Report Crashes

1. **Check crash dashboard**:
   ```bash
   curl https://your-backend-url.com/api/crashes | jq '.' | less
   ```

2. **Send me the crash data**:
   ```bash
   # Copy the JSON and send it to me (via email, GitHub issue, etc.)
   curl https://your-backend-url.com/api/crashes > latest_crashes.json
   ```

3. **I analyze and create a fix**:
   - I identify the crash root cause
   - I write code to fix it
   - I create a PR with tests

4. **You review and merge**:
   - Review my fix PR
   - Click "Merge" on GitHub
   - GitHub Actions auto-builds

5. **Users auto-update**:
   - Next time they launch the app
   - Update check runs automatically
   - New version downloads + extracts
   - Crash is fixed without any action from them

## Example Crash Flow

**Day 1 - User crashes:**
```
User launches app → NullReferenceException → Crash report sent to backend
```

**Day 2 - You monitor:**
```bash
curl https://backend-url/api/stats
# Returns:
# {
#   "totalCrashes": 3,
#   "crashTypes": {
#     "System.NullReferenceException": 3
#   }
# }
```

**Day 2 afternoon - I fix it:**
```
Send me crash logs → I analyze → Create PR with fix → You confirm
```

**Day 3 - GitHub Actions builds:**
```
Merge PR → GitHub Actions builds → Creates release → Users download
```

**Day 3 evening - Users are fixed:**
```
Users launch app → Sees "Update available" → Auto-downloads → Restarts → No more crash
```

## Key Benefits

| Feature | Benefit |
|---------|---------|
| **Auto-reporting** | Users don't need to manually send crash dumps |
| **Auto-updating** | Users don't need to manually download anything |
| **Backend dashboard** | You can see trends (which crashes are most common) |
| **CI/CD pipeline** | Automatic build + release (less manual work) |
| **Zero user friction** | Everything happens in background silently |

## Troubleshooting

### "Backend URL not reachable"
- Check URL is correct (starts with `https://`)
- Check backend hasn't crashed (visit URL in browser)
- Verify Railway/Render service is still running

### "Crashes not appearing"
- Confirm `HOLIDAY_SUPPORT_ENDPOINT` is set: `echo $HOLIDAY_SUPPORT_ENDPOINT`
- Check app is actually crashing (not catching exception silently)
- Verify backend is running: `curl https://backend-url/health`

### "Update not triggering"
- Confirm GitHub Actions workflow exists: Check `.github/workflows/build-release.yml`
- Push a code change to main: `git push origin main`
- Check "Actions" tab in GitHub to see build status

## Next Steps

1. **Deploy backend**: Pick Railway or Render (5 minutes)
2. **Configure app**: Set `HOLIDAY_SUPPORT_ENDPOINT` (2 minutes)
3. **Test**: Check health endpoint (1 minute)
4. **Release test version**: Push a small change to test the pipeline
5. **Monitor**: Check `/api/stats` periodically for crashes
6. **Send me crashes**: When issues arise, share the data from `/api/crashes`

Ready? Let me know the backend URL once deployed!
