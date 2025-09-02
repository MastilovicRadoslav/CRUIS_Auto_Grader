import random

questions = {
    "Koja je prijestonica Francuske?": "Pariz",
    "Koliko je 5 * 6?": "30",
    "Koji je najveći kontinent?": "Azija",
    "Koji je programski jezik poznat po zmiji u imenu?": "Python"
}

score = 0
print("Dobrodošli u kviz!\n")

for q, a in random.sample(list(questions.items()), len(questions)):
    print(q)
    answer = input("Odgovor: ")
    if answer.strip().lower() == a.lower():
        print("✅ Tačno!\n")
        score += 1
    else:
        print(f"❌ Netačno! Tačan odgovor je: {a}\n")

print(f"Osvojili ste {score}/{len(questions)} bodova.")
