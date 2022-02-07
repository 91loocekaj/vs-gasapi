# Gas API
## Introduction
I originally had the idea for making gases first as a feature for Useful Stuff, then went into the offshoot mod Lands of Chaos. The first system used special meta bloks to accomplish this, and while it somewhat worked, due to it being block based it had many flaws. Because it used blocks, no more than one gas could exist in a block position at a time and without a block entity it could not store unique data so gas blocks only had 8 levels, increments of 12.5%. Again due to the block based system, it had the massive potential to cause compatibility issues with mods and multiblock structures that expected air in certain positions, would leave behind a whole bunch of placeholder blocks if the mod was removed, and finally was prone to getting into infinite loops. Because of all this I decided to completely redo the system using chunk data instead of blocks.

While orginally going to be apart of Lands Of Chaos, with the new system using purely chunk data with primitive data types that can be accessed by any mod if it knows the proper location and structure, I decided to make a part api/library mod similiar to Goxmeor's Buff Stuff mod.
Thus, the offshoot of an offshoot, Asphyxia was born!

## Basics
Gases are stored as dictionaries with the gas name(string) as the key, and the concentration of the gas(a decimal value) as the value. Within a block space, this allows for the existence of an indefinite number of gases at a time, however concentrations can only be 0-1 in a block space. A gas spread event is when gases are spread and/or redistributed in an area. The maximum of that area is a cube that has sides which are 2 * radius + 1, so the default radius of 7 means a maximum spread area of 15x15x15. The spread starts from the center and will spread to blocks if it is not blocked by a solid side. Gases can exist in liquid blocks, but when spreading they will tend to stay out of liquid, or if they are in liquid, tend to get out. Blocks that are Plant or Leaves material will absorb certain gases like carbon dioxide. If there are more gases than the area can hold they will voided. Likewise if a position in the spread event is open to the sky the gas will also be voided if the wind is at a certain speed, but stored in the pollution of the chunk.

### Gas Spread Dictionary
In json a gas dictionary would like { "gasname": gas.value, "helium": 0.5}. In C# it is a Dictionary<string,float> object.

### Meta Keys
- "RADIUS" //This key will allow you to put in a custom radius based on the value it is assigned
- "THISISANEXPLOSION" //This key sets the gas spread event to turn flammable/combustible gases to their burned forms if they have one. The value assigned does not matter
- "THISISAPLANT" //This key will act like a plant and absorb plant absorbable gases based on the value it is assigned.
- "IGNORELIQUIDS" //This key will cause gas spread event to ignore the stay out of liquids rule and will spread freely through whatever medium. Value does not matter
- "IGNORESOLIDCHECK" //This key will cause gas spread event to ignore solid sides entirely. Value does not matter


## Content Side (JSON)
The properties of gases are defined in gasapi:config/gases.json, and by patching existing gas properties can be changed or you can add your own gas. There are also a handful of behaviors that can be used and modified as well.

### Gas Properties

The key for your gas should be the same as it's code.

- "light": false //This determines if a gas will sink or float. Light gases collect at the ceiling, heavy gases pool on the ground
- "distribute": false //If true, this gas will ignore gravity and will evenly distribute in the entire area.
- "ventilateSpeed": 0 //This determines at what wind speed gas will disappear if exposed to open sky. 1.85 is the fastest wind blows
- "plantAbsorb": false //If true this gas will be absorbed by plants
- "suffocateAmount": 1 //At what value this gas will suffocate the player.
- "flammableAmount": 2 //At what value this will cause entities that are in it to burn if there is an explosion. Must be less than or equal to 1 to burn
- "explosionAmount": 2 //At what value this gas will explode. Must less than or equal to 1 to explode
- "burnInto": "null" //If this is flammable or explosive and the gas spreading event is combustive, this is what gas it will turn into
- "acidic": false //If true this gas will turn liquids acidic and its pollution will be acidic
- "effects": {"walkspeed": -0.2} // The effects this gas has on creatures that breathe it. So at 50% concentraion this gas will reduce movement speed by 10%. At full concentration it will reduce movement speed by 20%
- "toxicAt": 0 //At what point will this gas start applying its affects
- "pollutant": false // If true this gas will cause greenhouse effects and acid rain effects if acidic. (Not yet implemented)

To give your gas a display name, simply put "gasapi:gas-yourgascode": "yourgasdisplayname" in your lang file.

### Block Behaviors
These block behaviors can be used to create and spread gases in the world

- "SparkGas": Blocks with this behavior has a chance to detonate explosive gases
  - Can be used with any block
  - Has no definable properties

- "MineGas": Blocks with this behavior produce gas when broken
  - Can be used with any block
  - "produceGas" Is a gas dictionary of what to spread, "onRemove" is true/false whether it should happen when the block is broken or when removed

- "PlaceGas": Blocks with this behavior has a chance to detonate explosive gases
  - Can be used with any block, preferably with at least one non solid side
  - "produceGas" Is a gas dictionary of what to spread

- "ExplosionGas": Blocks with this behavior release gas when blown up
  - Can be used with any block
  - "produceGas" Is a gas dictionary of what to spread

### Block Entity Behaviors
These block behaviors can be used to create and spread gases in the world

- "BurningProduces": Block entities with this behavior will produce gas if they are burning
  - Only works with the firepit, forge, bloomery, coal pile, torches, torch holders, pit kilns, charcoalpits, and boilers
  - "produceGas" Is a gas dictionary of what to spread. By dewfault this is {"carbonmonoxide": 0.2, "carbondioxide": 0.5}

- "PlanterAbsorbs": Block entities with this behavior absorb plant absorbent gases
  - Only works with flowerpots and planters
  - "produceGas" Is a gas dictionary of what to spread. Default is {"THISISAPLANT" : 1}

- "ProduceGas": Block entities with this behavior produce gas
  - Works with any block entity
  - "produceGas" Is a gas dictionary of what to spread. "updateMS" is an integer of how often in milliseconds the production function is called. "updateHours" is in game hourly intervals it spawns gas

### Entity Behaviors
These are behaviors that can be given to entities to interact with the gas system

- "breathe": This replaces the vanilla behavior. Requires entities to need air and makes them have toxic effects from gases
  - Works for all entities with "health"
  - "waterBreather" If true this will make the entity breath in liquids but suffocate on land. "currentair" is a decimal amount of how much air the entity starts with at spawn. "maxair" Is the maximum amount of time an entity can hold its breath.

### Special Item Attributes
These attributes can give certain items special gas-related properties

- "gassysAntiCorrosion": true //This attribute is for armor, and if set to true makes it so that they do not corrode. See armor patches
- "gassysGasMaskProtection": [ "nitrogendioxide" ] //This attribute is a string array for masks, and will protect the user from toxic gases that are specified in it. The mask will be damaged when in the presence of those gases
- "gassysScubaMask": true //This attribute when true on a mask, makes it a part of a scuba set. Needs a scuba tank to work
- "gassysScubaTank": true //This attribute when true on an item in the second slot of a person's inventory, will act like a scuba tank and when combined with the mask, will provide a breathable air and completely protect against toxic effects. Takes damage as long as the mask is worn.

### Special Block Attributes
The attributes can give certain blocks special gas related properties

- "gassysSolidSides": {"north": true, "south": false} // If this exists in a blocks attributes, during a gas spread event, its actual solid sides is ignored and this is considered
- "gassysPlant": true // If this exists the gas spread event will ignore its block material check and instead check this to see if it is a plant.

## Code Side (C#)
Due to the new system using primitive data in stored in chunk data that any mod can access, it is not neccessary to make this mod a hard depedency. Best practice to use this library, is to copy and paste the [GasHelper class](src/For Other Mods/GasHelper.cs) into your own name space. Then retrieve it with ICoreAPI.Modloader.GetModSystem and use it like any other mod system.

### Data Storage
Gas data is stored in the chunk mod data in the key "gases". For a chunk it is a Dictionary<int, Dictionary<string, float>>. The key is the local block position index and the values are the gas dictionary for the particular block position. Modifying, adding, and deleting data should all occur server side, and then should be synced on the client. No editing should happen on the client.

### The Gas Helper
The GasHelper is a helper class designed to make manipulation of gases easier for other mods. All of its methods will not function and return default values if the main Gas API mod is not installed or not enabled, so you can use these methods without having to check for if the mod is there. The gas helper can only read gas information and has no writing ability. To cause a gas spread event it will turn the data into a TreeAttribute object and send it to the main Gas API via the event bus to process. If updates need to be made to it the version number will change, so make sure to keep track of that when new updates of the mod come out.

### Limitations
Gas spreading takes place on its own separate thread than the main game one. This helps tremendously with performance vs the old system, but comes at the cost that it is not in sync with the main game thread. So if you where to send a gas spread event and then in the next line check what gases are at the origin of the gas spread, your gases will most likely not be there yet.

## Other Stuff

### Commands
All commands start with /gassys and then the keyword.
- queue: Shows how many gas spreading events are scheduled to run
- reset: Recreates the queue
- find: Shows the positions for all gas spread events in queue
- stop: Shuts down the gas spread thread
- start: Starts up the gas spread thread
- cleanstart: Starts up the gas spread queue with a new, empty queue
- toggle: Toggles the pause on the gas spread thread
- pollution: Shows how much pollution is in the chunk

### Todo
The pollution system right now only records gases that have been vented, none of the values are actually used for climate change and things of that nature. Planning on doing something with that once I figure out a good balance.
