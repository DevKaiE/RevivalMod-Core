        // Change this ID to a defibrillator if available, or keep it as a bandage for testing
        // Defibrillator ID from Escape from Tarkov: "5c052e6986f7746b207bc3c9"
        // Personal medkit ID: "5e99711486f7744bfc4af328"
        // CMS kit ID: "5d02778e86f774203e7dedbe"
        // Bandage ID for testing: "544fb25a4bdc2dfb738b4567"
export interface Trader {
    id: string;
    name: string;
}

export const ITEM_ID = "5c052e6986f7746b207bc3c9";
export const Prapor: Trader = {id: "54cb50c76803fa8b248b4571", name: "Prapor"};
export const Therapist: Trader = {id: "54cb57776803fa99248b456e", name: "Therapist"};
export const Fence: Trader = {id: "579dc571d53a0658a154fbec", name: "Fence"};
export const Skier: Trader = {id: "58330581ace78e27b8b10cee", name: "Skier"};
export const Peacekeeper: Trader = {id: "5935c25fb3acc3127c3d8cd9", name: "Peacekeeper"};
export const Mechanic: Trader = {id: "5a7c2eca46aef81a7ca2145d", name: "Mechanic"};
export const Ragman: Trader = {id: "5ac3b934156ae10c4430e83c", name: "Ragman"};
export const Jaeger: Trader = {id: "5c0647fdd443bc2504c2d371", name: "Jaeger"};
export const Lighthousekeeper: Trader = {id: "638f541a29ffd1183d187f57", name: "Lighthousekeeper"};

export const TRADERS: Trader[] = [Prapor, Therapist, Fence, Skier, Peacekeeper, Mechanic, Ragman, Jaeger, Lighthousekeeper];

