# services/ai-service/Dockerfile

FROM node:20

WORKDIR /app

COPY package*.json ./
RUN npm install

COPY . .

EXPOSE 6082

CMD ["npm", "run", "dev"]
