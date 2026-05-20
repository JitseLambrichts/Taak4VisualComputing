# Verslag

## Implementatie plan
We zijn begonnen met een heel simpele opzet door eerst 2 bakjes te ontwerpen, en gewoon een zwevende grijper. Hierna hebben we ervoor gezorgd dat de blokjes, op een willekeurige positie gespawned werden in eenzelfde bakje. Wanneer dit werkte, hebben we een C# script geschreven dat ervoor zorgde dat de grijper de blokjes kon beginnen opnemen. Eerst gebeurde dit via een direct pad (x, y en z coordinaten tegelijkertijd), waarna we dit hebben moeten opsplitsen in de verschillende richtingen (x, y, z apart). Wanneer dit volledig functioneerde, hebben we het frame van de arm volledig gebouwd. Wanneer de volledige constructie in orde was, moesten we nog de verschuifbare componenten van de robot arm (horizontale en verticale baar) mee laten verplaatsen.

## Fysische wereld
* **Physics & Zwaartekracht:** De blokjes worden boven de ophaalbak gespawnd door de `BlockSpawner` en vallen met behulp van Unity's ingebouwde zwaartekracht (wat mogelijk werd gemaakt dankzij een onderdeel van `Rigidbody`) realistisch in de ophaalbak.
* **Synchronisatie (Start Delay):** De spawner genereert maximaal 6 blokjes met een interval van 1,5 seconde. Om te voorkomen dat de robotarm begint te bewegen terwijl blokjes nog aan het vallen zijn, is een `startDelay` van 9 seconden ingesteld in `ArmController`. Pas daarna begint de robot met bewegen

## State machine
Het gedrag van de robotarm is volledig gestructureerd via een Finite State Machine (FSM):

```
       [ FindBlock ] ◄────────────────────────────────────────┐
             │ (Blok gevonden)                                │
             ▼                                                │
       [ MoveToBlock ]                                        │
             │ (Boven blok gearriveerd)                       │
             ▼                                                │
         [ PickUp ]                                           │
             │ (Blok vastgegrepen)                            │
             ▼                                                │
    [ MoveUpAfterPickup ]                                     │ (Volgend blok zoeken)
             │ (Veilig omhoog geheven)                        │
             ▼                                                │
       [ MoveToDrop ]                                         │
             │ (Boven gridpositie gearriveerd)                │
             ▼                                                │
          [ Drop ] ───────────────────────────────────────────┘
```

* **FindBlock:** Blokje zoeken binnen de pickup area.
* **MoveToBlock:** Grijper horizontaal (X, Z) verplaatsen en vervolgens verticaal dalen (Y) tot net boven het blokje.
* **Pickup:** Het blokje fysiek koppelen aan de grijper (physics uitschakelen, isKinematic aanzetten, en parent instellen) zonder verdere beweging.
* **MoveAfterPickup:** De grijper met het blokje weer verticaal (Y) omhoog heffen.
* **MoveToDrop:** Horizontaal (X, Z) bewegen naar de berekende drop-off locatie boven het afzetbakje.
* **Drop:** Het blokje loskoppelen (physics inschakelen, zwaartekracht aanzetten) zodat het netjes in het grid van de bak valt.

## Problemen
Een van de moeilijkheden die we ondervonden was de objecten, voornamelijk van de robotarm, goed laten aligneren met de andere componenten. Dit was vooral omdat deze niet automatisch wouden 'snappen' tegen de rest. \
Eerst hadden we het probleem dat deze ook de blokjes die de robot net had gedropped, dat de robot dit blokje opnieuw zag als dichtstbijzijnde blokje. Daarom hebben we geïmplementeerd dat deze alleen blokjes mag opnemen in de pickup area.