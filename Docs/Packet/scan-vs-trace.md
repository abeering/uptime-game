## 2. Scan vs Trace

### Scan
Reveals:
- stage
- class
- kind
- keywords
- infection

→ answers: “what is this?”

---

### Trace
Reveals:
- source IP
- destination node

→ answers: “what pattern is this part of?”

---

### Design Intent

Scan = classification  
Trace = context  
Block = action  

---

## 3. Why Trace Matters

Trace enables strategic play, not just info:

### A. Rule-based blocking
block ip=2.2.2.1  
block dest=cache  

→ shifts from reactive → systemic defense

---

### B. Attack pattern recognition
Multiple packets share:
src=2.2.2.1 dest=cache  

→ player detects coordinated attack

---

### C. Target prioritization
- Identify high-value nodes under attack
- Preemptively defend
