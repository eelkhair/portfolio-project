import express from 'express';
import cors from 'cors';
const { swaggerUi, swaggerSpec } = require('../settings/swagger');

const app = express();
app.use(cors());
app.use(express.json());

// 👉 Dapr topic discovery
app.get('/dapr/subscribe', (_req, res) => {
    // Return [] if you have no pub/sub yet.
    // When you add topics, return an array of { pubsubname, topic, route } objects.
    res.type('application/json').send([]);
});

/**
 * @swagger
 * /api/ai/hello:
 *   get:
 *     summary: test swagger
 *     responses:
 *       200:
 *         description: success
 */
app.get('/api/ai/hello', (_req, res) => {
    res.json({ message: 'Hello from AI service!' });
});

/**
 * @swagger
 * /process-text:
 *   post:
 *     summary: Process a text input
 *     requestBody:
 *       required: true
 *       content:
 *         application/json:
 *           schema:
 *             type: object
 *             required:
 *               - text
 *             properties:
 *               text:
 *                 type: string
 *     responses:
 *       200:
 *         description: Text successfully processed
 */
app.post('/process-text', (req, res) => {
    const { text } = req.body;
    res.json({ result: `Processed: ${text}` });
});

// Swagger last so it doesn't intercept /dapr/* probes
app.use('/', swaggerUi.serve, swaggerUi.setup(swaggerSpec));

const port = Number(process.env.PORT) || 6082;
app.listen(port, '0.0.0.0', () => {
    console.log(`AI service running on port ${port}`);
});
