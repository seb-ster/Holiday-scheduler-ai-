# Holiday Scheduler Backend Deployment

This backend collects crash reports and feedback from Holiday Scheduler users automatically.

## Features

- **Automatic crash collection**: Receives stack traces, exception types, OS info, versions
- **Feedback collection**: Stores user feedback with categories
- **Analytics dashboard**: View crash statistics and trends
- **Zero user interaction**: Reports sent automatically in background

## Quick Start (Local Development)

```bash
cd backend
npm install
npm start
```

Server runs on http://localhost:3000

## Deployment Options

### Option 1: Railway (Recommended - Free tier)

1. **Create Railway account**: https://railway.app
2. **Connect GitHub repository**:
   - Click "New Project"
   - Select "Deploy from GitHub repo"
   - Authorize connection
   - Select this repo

3. **Configure environment**:
   - Railway auto-detects Node.js
   - No config needed; uses defaults

4. **Get deployment URL**:
   - Once deployed, you get a URL like `https://holiday-scheduler-backend-prod.up.railway.app`

5. **Set app endpoint**:
   ```bash
   export HOLIDAY_SUPPORT_ENDPOINT=https://holiday-scheduler-backend-prod.up.railway.app
   # Then run the app
   ```

### Option 2: Render (Free tier)

1. Go to https://render.com
2. Click "New +" → "Web Service"
3. Connect GitHub repo
4. Select `backend` as root directory
5. Build command: `npm install`
6. Start command: `npm start`
7. Deploy

Get URL from deployment dashboard.

### Option 3: Vercel (Simplest)

1. Go to https://vercel.com
2. Import GitHub repo
3. Select `backend` directory
4. Deploy

## App Configuration

Once backend is deployed, configure the app to send reports:

```bash
# Set environment variable
export HOLIDAY_SUPPORT_ENDPOINT=https://your-backend-url.com

# Then launch app
/Applications/Holiday\ Scheduler\ Demonstrator.app/Contents/MacOS/Holiday\ Scheduler\ Demonstrator
```

## Monitoring Crashes

Once backend is running, view crash stats:

```bash
# View recent crashes
curl https://your-backend-url/api/crashes

# View statistics
curl https://your-backend-url/api/stats

# Example output:
{
  "totalCrashes": 5,
  "totalFeedback": 2,
  "crashTypes": {
    "System.NullReferenceException": 3,
    "System.IO.FileNotFoundException": 2
  }
}
```

## Analyzing Crashes

Look for patterns in crash types and versions. Send me the crash data and I can:
1. Diagnose root causes
2. Generate fixes
3. Test via GitHub Actions
4. Auto-release updates

## Data Storage

- Crash reports: `backend/data/crashes/`
- Feedback: `backend/data/feedback/`
- Both stored as JSON files with timestamps

## Production Considerations

- **Backups**: Set up daily backups of `data/` directory
- **Privacy**: Store reports securely; user data is included
- **Rate limiting**: Add if backend gets high traffic
- **Database migration**: Graduate from JSON to MongoDB/PostgreSQL later if needed

## Testing

```bash
# Send test crash report
curl -X POST http://localhost:3000/api/crash \
  -H "Content-Type: application/json" \
  -d '{
    "version": "1.0.0",
    "os": "macOS",
    "exceptionType": "TestException",
    "message": "Test crash",
    "stackTrace": "at Main()"
  }'

# Response:
# {"success": true, "id": "crash-1234567890-abcd1234.json"}
```

## Next Steps

1. Deploy backend to Railway/Render/Vercel
2. Note the deployment URL
3. Update app with `HOLIDAY_SUPPORT_ENDPOINT`
4. Release new version
5. Monitor crash dashboard
6. When crashes appear, send me the data for analysis

Let me know which hosting platform you prefer!
