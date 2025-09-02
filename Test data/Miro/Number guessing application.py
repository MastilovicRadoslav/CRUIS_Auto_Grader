import random

print("🎲 Pogodi broj od 1 do 100!")
secret = random.randint(1, 100)
attempts = 0

while True:
    guess = input("Unesi broj: ")
    if not guess.isdigit():
        print("Molim unesi broj.")
        continue

    guess = int(guess)
    attempts += 1

    if guess < secret:
        print("🔼 Moj broj je veći.")
    elif guess > secret:
        print("🔽 Moj broj je manji.")
    else:
        print(f"🎉 Bravo! Pogodio si u {attempts} pokušaja.")
        break
