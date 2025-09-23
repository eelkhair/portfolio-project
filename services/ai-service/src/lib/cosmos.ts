import { CosmosClient, Container } from "@azure/cosmos";

let container: Container | null = null;
export function getContainer(): Container {
    if (container) return container;

    const client = new CosmosClient({
        endpoint: process.env.COSMOS_ENDPOINT!,
        key: process.env.COSMOS_KEY!,
        // stay on your proxy domain (emulator advertises 127.0.0.1)
        connectionPolicy: { enableEndpointDiscovery: false }
    });

    container = client.database(process.env.COSMOS_DB!).container(process.env.COSMOS_CONTAINER!);
    return container;
}
