import { CosmosClient, Container } from "@azure/cosmos";
import {z} from "zod";
import {JobDraft} from "../schemas/job-draft.js";
import {JobPayloadType} from "../schemas/job-published-event.js";
type JobDraftT = z.infer<typeof JobDraft>;
export class CosmosService{

    private client: CosmosClient;
    constructor() {
        this.client = new CosmosClient({
            endpoint: process.env.COSMOS_ENDPOINT!,
            key: process.env.COSMOS_KEY!,
            connectionPolicy: { enableEndpointDiscovery: false }
        });
    }

    async saveDraft(companyId:string, result: JobDraftT){
        const container = this.getContainer("drafts");
        return (await container.items.upsert({...result, companyId})).resource?.id;
    }

    async deleteDraft(id: string, companyId:string) {
        const container = this.getContainer("drafts");
        await container.item(id, companyId).delete();
    }

    async listDrafts(companyId:string){
        const container = this.getContainer("drafts");
        const querySpec = {
            query: "SELECT * FROM c WHERE c.companyId = @companyId",
            parameters: [
                { name: "@companyId", value: companyId }
            ]
        };

        const { resources } = await container.items.query(querySpec).fetchAll();
        return resources as JobDraftT[];

    }

    async upsertJob(job: JobPayloadType){
        const container = this.getContainer("jobs");
        await container.items.upsert({...job, companyId: job.companyUId})
    }

    private getContainer(container:string): Container {
        return this.client.database(process.env.COSMOS_DB!).container(container)
    }
}

