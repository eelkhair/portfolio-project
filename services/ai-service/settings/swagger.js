const swaggerJsdoc = require('swagger-jsdoc');
const swaggerUi = require('swagger-ui-express');

const options = {
    definition: {
        openapi: '3.0.0',
        info: {
            title: 'Job AI',
            version: '1.0.0',
            description: 'API for managing AI operations',
        },
    },
    apis: ['./src/**/*.ts'], // 👈 Add this to include your TypeScript files
    // or if you're compiling to JS, use: ['./dist/**/*.js']
};

const swaggerSpec = swaggerJsdoc(options);

module.exports = { swaggerUi, swaggerSpec };
