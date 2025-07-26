import express from 'express';
import cors from 'cors';
const { swaggerUi, swaggerSpec } = require( '../settings/swagger')
const app = express();
app.use(cors());
app.use(express.json());

/**
 * @swagger
 * /api/ai/hello:
 *   get:
 *     summary: test swagger
 *     responses:
 *       200:
 *         description: success
 */
app.get('/api/ai/hello', (req, res) => {
    res.json({ message: 'Hello from AI service!' });
});

/**
 * @swagger
 * /process-text:
 *   post:
 *     summary: Process a text input
 *     description: Takes a text input and returns it with a "Processed:" prefix
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
 *                 description: The text to be processed
 *     responses:
 *       200:
 *         description: Text successfully processed
 *         content:
 *           application/json:
 *             schema:
 *               type: object
 *               properties:
 *                 result:
 *                   type: string
 *                   description: The processed text with prefix
 *       400:
 *
 */

 app.post('/process-text', (req, res) => {
    const { text } = req.body;
    res.json({ result: `Processed: ${text}` });

});

app.use('/', swaggerUi.serve, swaggerUi.setup(swaggerSpec));

const port = Number(process.env.PORT) || 6082;
app.listen(port, '0.0.0.0', () => {
    console.log(`AI service running on port ${port}`);
});

