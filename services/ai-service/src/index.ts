import express from 'express';
import cors from 'cors';

const app = express();
app.use(cors());
app.use(express.json());

// ✅ Example GET endpoint
app.get('/api/ai/hello', (req, res) => {
    res.json({ message: 'Hello from AI service!' });
});

// ✅ Example POST endpoint for Dapr service invocation
app.post('/process-text', (req, res) => {
    const { text } = req.body;
    res.json({ result: `Processed: ${text}` });
});

// ✅ Fix: Ensure port is a number AND binds to all interfaces
const port = Number(process.env.PORT) || 6082;
app.listen(port, '0.0.0.0', () => {
    console.log(`AI service running on port ${port}`);
});
