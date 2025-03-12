import { DependencyContainer } from "tsyringe";
import { IPreSptLoadMod } from "@spt/models/external/IPreSptLoadMod";
import { IPostSptLoadMod } from "@spt/models/external/IPostSptLoadMod";
import { IPostDBLoadMod } from "@spt/models/external/IPostDBLoadMod";
import { DatabaseServer } from "@spt/servers/DatabaseServer";
import { SaveServer } from "@spt/servers/SaveServer";
import { StaticRouterModService } from "@spt/services/mod/staticRouter/StaticRouterModService";
import { ILogger } from "@spt/models/spt/utils/ILogger";
import { ITEM_ID, Prapor, Therapist, Trader, TRADERS } from "./constants";
import { HttpResponseUtil } from "@spt/utils/HttpResponseUtil";
import { JsonUtil } from "@spt/utils/JsonUtil";
import config from "./config.json";

class RevivalMod implements IPreSptLoadMod, IPostDBLoadMod, IPostSptLoadMod {
    private mod: string = "RevivalMod";
    // Using the defibrillator item ID
    private defibId: string = ITEM_ID;
    private trader: Trader = TRADERS.find(trader => trader.name === config.RevivalItem.Trading.Trader) || Therapist;
    
    // SPT Services
    private databaseServer: DatabaseServer;
    private saveServer: SaveServer;
    private logger: ILogger;
    private staticRouterModService: StaticRouterModService;
    private httpResponseUtil: HttpResponseUtil;
    private jsonUtil: JsonUtil;

    public preSptLoad(container: DependencyContainer): void {
        this.logger = container.resolve<ILogger>("WinstonLogger");
        this.logger.info(`[${this.mod}] Starting to load...`);
    }

    public postDBLoad(container: DependencyContainer): void {
        this.logger.info(`[${this.mod}] Database loaded, setting up mod...`);

        try {
            // Resolve services
            this.databaseServer = container.resolve<DatabaseServer>("DatabaseServer");
            this.saveServer = container.resolve<SaveServer>("SaveServer");
            this.httpResponseUtil = container.resolve<HttpResponseUtil>("HttpResponseUtil");
            this.jsonUtil = container.resolve<JsonUtil>("JsonUtil");

            // Add the defibrillator to Prapor level 1
            this.addDefibrillatorToTrader();
            
            // Optional: Make the defibrillator more affordable
            this.adjustDefibrillatorPrice();

            // Optional: Improve the defibrillator's properties
            this.enhanceDefibrillatorProperties();

            this.logger.info(`[${this.mod}] Setup complete in postDBLoad`);
        } catch (error) {
            this.logger.error(`[${this.mod}] Error in postDBLoad: ${error.message}`);
            if (error.stack) {
                this.logger.error(`[${this.mod}] Stack trace: ${error.stack}`);
            }
        }
    }

    public postSptLoad(container: DependencyContainer): void {
        this.logger.info(`[${this.mod}] Post AKI load, setting up callbacks...`);

        try {
            // Resolve the static router service
            this.staticRouterModService = container.resolve<StaticRouterModService>("StaticRouterModService");
            
            // Debug trader inventory
            this.debugTraderInventory();

            // Register the callbacks
            this.registerCallbacks();

            this.logger.info(`[${this.mod}] Mod initialization complete`);
        } catch (error) {
            this.logger.error(`[${this.mod}] Error in postAkiLoad: ${error.message}`);
            if (error.stack) {
                this.logger.error(`[${this.mod}] Stack trace: ${error.stack}`);
            }
        }
    }

    private addDefibrillatorToTrader(): void {
        this.logger.info(`[${this.mod}] Adding Defibrillator to ${this.trader.name}'s inventory...`);

        try {
            const tables = this.databaseServer.getTables();

            if (!tables || !tables.traders) {
                this.logger.error(`[${this.mod}] Traders table is undefined`);
                return;
            }

            // Get Prapor's assort
            const traderTables = tables.traders[this.trader.id];
            if (!traderTables) {
                this.logger.error(`[${this.mod}] Could not find Prapor trader in table`);
                return;
            }

            this.logger.info(`[${this.mod}] Found Prapor trader, adding revival item`);

            // Generate a proper 24-character hex MongoDB ID for item in Prapor's inventory
            const uniqueItemId = "60dc0d93a66c41234a80aeff"; // Random but consistent ID

            this.logger.info(`[${this.mod}] Using trader item ID: ${uniqueItemId}`);

            // Make sure trader has all the required properties
            if (!traderTables.assort) {
                traderTables.assort = {
                    nextResupply: 0,
                    items: [],
                    barter_scheme: {},
                    loyal_level_items: {}
                };
            }
            
            // Check if item already exists and remove it to ensure clean installation
            if (traderTables.assort.items) {
                traderTables.assort.items = traderTables.assort.items.filter(item => item._id !== uniqueItemId);
            }

            // Add to items array
            traderTables.assort.items.push({
                _id: uniqueItemId,
                _tpl: this.defibId,
                parentId: "hideout",
                slotId: "hideout",
                upd: {
                    UnlimitedCount: true,
                    StackObjectsCount: 999999
                }
            });

            // Add barter scheme (price in rubles)
            traderTables.assort.barter_scheme[uniqueItemId] = [
                [
                    {
                        count: config.RevivalItem.Trading.AmountRoubles || 25000, // 25,000 rubles
                        _tpl: "5449016a4bdc2d6f028b456f" // Roubles
                    }
                ]
            ];

            // Add loyalty level requirement
            traderTables.assort.loyal_level_items[uniqueItemId] = 1; // Level 1

            this.logger.info(`[${this.mod}] Successfully added Defibrillator to ${this.trader.name}'s inventory`);
            
        } catch (error) {
            this.logger.error(`[${this.mod}] Error adding Revival Item to ${this.trader.name}'s inventory: ${error.message}`);
            if (error.stack) {
                this.logger.error(`[${this.mod}] Stack trace: ${error.stack}`);
            }
        }
    }

    private adjustDefibrillatorPrice(): void {
        // Also adjust the main item in the templates to be more reasonably priced
        try {
            const tables = this.databaseServer.getTables();
            const items = tables.templates.items;
            
            // Adjust base price of defibrillator for flea market
            if (items[this.defibId]) {
                items[this.defibId]._props.CreditsPrice = 25000;
                this.logger.info(`[${this.mod}] Adjusted base price of defibrillator to 25,000 ₽`);
            }
        } catch (error) {
            this.logger.error(`[${this.mod}] Error adjusting defibrillator price: ${error.message}`);
        }
    }

    private enhanceDefibrillatorProperties(): void {
        try {
            const tables = this.databaseServer.getTables();
            const items = tables.templates.items;
            
            // Get the defibrillator item
            if (items[this.defibId]) {
                // Update description to mention the revival mod
                items[this.defibId]._props.Description = 
                    "A portable defibrillator used to revive yourself or others from critical condition. " + 
                    "When in critical state, press F5 to use and get a second chance at life.";
                
                // Make it more compact (2x1 instead of 2x2)
                if (items[this.defibId]._props.Width && items[this.defibId]._props.Height) {
                    items[this.defibId]._props.Width = 2;  
                    items[this.defibId]._props.Height = 1;
                }
                
                // Add a special property to make the defibrillator clearly visible
                if (!items[this.defibId]._props.BackgroundColor) {
                    items[this.defibId]._props.BackgroundColor = "red";
                }
                
                this.logger.info(`[${this.mod}] Enhanced defibrillator properties for better use with RevivalMod`);
            }
        } catch (error) {
            this.logger.error(`[${this.mod}] Error enhancing defibrillator properties: ${error.message}`);
        }
    }

    private debugTraderInventory(): void {
        this.logger.info(`[${this.mod}] Debugging ${this.trader.name}'s inventory...`);

        try {
            const tables = this.databaseServer.getTables();

            if (!tables || !tables.traders || !tables.traders[this.trader.id]) {
                this.logger.error(`[${this.mod}] ${this.trader.name} data not available for debugging`);
                return;
            }

            const traderTables = tables.traders[this.trader.id];

            this.logger.info(`[${this.mod}] DEBUG: ${this.trader.name} has ${traderTables.assort?.items?.length || 0} items`);

            // Check if our item is in Prapor's inventory
            const defib = traderTables.assort?.items?.find(item => item._tpl === this.defibId);
            if (defib) {
                this.logger.info(`[${this.mod}] DEBUG: Defibrillator found in Prapor's inventory with ID: ${defib._id}`);

                // Check if it has a barter scheme
                if (traderTables.assort?.barter_scheme?.[defib._id]) {
                    this.logger.info(`[${this.mod}] DEBUG: Defibrillator has a barter scheme`);
                    
                    // Log the price
                    const priceData = traderTables.assort.barter_scheme[defib._id][0][0];
                    this.logger.info(`[${this.mod}] DEBUG: Defibrillator price: ${priceData.count} roubles`);
                } else {
                    this.logger.warning(`[${this.mod}] DEBUG: WARNING - Defibrillator has no barter scheme!`);
                }

                // Check if it has a loyalty level
                if (traderTables.assort?.loyal_level_items?.[defib._id] !== undefined) {
                    this.logger.info(`[${this.mod}] DEBUG: Defibrillator has loyalty level: ${traderTables.assort.loyal_level_items[defib._id]}`);
                } else {
                    this.logger.warning(`[${this.mod}] DEBUG: WARNING - Defibrillator has no loyalty level!`);
                }
            } else {
                this.logger.warning(`[${this.mod}] DEBUG: Defibrillator NOT found in Prapor's inventory!`);
            }
        } catch (error) {
            this.logger.error(`[${this.mod}] Error debugging ${this.trader.name} inventory: ${error.message}`);
        }
    }

    private registerCallbacks(): void {
        this.logger.info(`[${this.mod}] Registering callback hooks...`);

        try {
            if (!this.staticRouterModService) {
                this.logger.error(`[${this.mod}] StaticRouterModService is undefined`);
                return;
            }

            // Register server-to-client callback
            this.staticRouterModService.registerStaticRouter(
                "RevivalModRouter",
                [
                    {
                        url: "/kaikinoodles/revivalmod/data_to_client",
                        action: async (url, info, sessionID) => {
                            this.logger.info(`[${this.mod}] Processing data to client request`);
                            return this.httpResponseUtil.getBody({
                                status: "ok",
                                message: "Server received data",
                                data: info
                            });
                        }
                    },
                    {
                        url: "/kaikinoodles/revivalmod/data_to_server",
                        action: async (url, info, sessionID) => {
                            this.logger.info(`[${this.mod}] Received data from client: ${this.jsonUtil.serialize(info)}`);
                            
                            // Process player data here (e.g., record revival events, sync between players)
                            
                            return this.httpResponseUtil.getBody({
                                status: "ok", 
                                message: "Data received by server"
                            });
                        }
                    }
                ],
                "aki"
            );
            
            this.logger.info(`[${this.mod}] Successfully registered callbacks`);
            
        } catch (error) {
            this.logger.error(`[${this.mod}] Error registering callbacks: ${error.message}`);
            if (error.stack) {
                this.logger.error(`[${this.mod}] Stack trace: ${error.stack}`);
            }
        }
    }
}

module.exports = { mod: new RevivalMod() };