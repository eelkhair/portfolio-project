import {FastifyInstance} from "fastify";

export default async function daprRoutes(app: FastifyInstance) {
    app.addHook('onRoute', (r) => {
        r.schema ??= {hide:true};
    });
    app.get("/dapr/subscribe", async () => []); // already returning []
    app.get("/dapr/config", async () => ({})); // empty config is fine
}
