const express = require('express');
const fs = require('fs');
const path = require('path');
const crypto = require('crypto');
const app = express();

app.use(express.json({ limit: '10mb' }));

const dataDir = path.join(__dirname, 'data');
const crashesDir = path.join(dataDir, 'crashes');
const feedbackDir = path.join(dataDir, 'feedback');

[dataDir, crashesDir, feedbackDir].forEach(dir => {
  if (!fs.existsSync(dir)) fs.mkdirSync(dir, { recursive: true });
});

app.get('/health', (req, res) => {
  res.json({ status: 'ok', timestamp: new Date().toISOString() });
});

app.post('/api/crash', (req, res) => {
  try {
    const report = req.body;
    const filename = `crash-${Date.now()}-${crypto.randomBytes(4).toString('hex')}.json`;
    fs.writeFileSync(path.join(crashesDir, filename), JSON.stringify(report, null, 2));
    console.log(`[${new Date().toISOString()}] Crash: ${report.exceptionType} (v${report.version})`);
    res.json({ success: true, id: filename });
  } catch (err) {
    console.error('Crash handler error:', err);
    res.status(500).json({ success: false, error: err.message });
  }
});

app.post('/api/feedback', (req, res) => {
  try {
    const report = req.body;
    const filename = `feedback-${Date.now()}-${crypto.randomBytes(4).toString('hex')}.json`;
    fs.writeFileSync(path.join(feedbackDir, filename), JSON.stringify(report, null, 2));
    console.log(`[${new Date().toISOString()}] Feedback: ${report.payload?.category}`);
    res.json({ success: true, id: filename });
  } catch (err) {
    console.error('Feedback handler error:', err);
    res.status(500).json({ success: false, error: err.message });
  }
});

app.get('/api/crashes', (req, res) => {
  try {
    const files = fs.readdirSync(crashesDir).sort().reverse().slice(0, 50);
    const crashes = files.map(f => JSON.parse(fs.readFileSync(path.join(crashesDir, f))));
    res.json({ crashes, count: crashes.length });
  } catch (err) {
    res.status(500).json({ error: err.message });
  }
});

app.get('/api/stats', (req, res) => {
  try {
    const crashes = fs.readdirSync(crashesDir);
    const feedback = fs.readdirSync(feedbackDir);
    
    const crashTypes = {};
    crashes.forEach(f => {
      const type = JSON.parse(fs.readFileSync(path.join(crashesDir, f))).exceptionType;
      crashTypes[type] = (crashTypes[type] || 0) + 1;
    });

    res.json({
      totalCrashes: crashes.length,
      totalFeedback: feedback.length,
      crashTypes,
      timestamp: new Date().toISOString()
    });
  } catch (err) {
    res.status(500).json({ error: err.message });
  }
});

const PORT = process.env.PORT || 3000;
app.listen(PORT, () => {
  console.log(`Backend listening on :${PORT}`);
});
