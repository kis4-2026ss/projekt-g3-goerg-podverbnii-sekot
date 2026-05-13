# Grader Tool Solution

## Goal of the Project
### What is the high-level goal of your project?
The primary goal is to engineer an Automated Pipeline that uses Large Language Models (LLMs) to provide instantaneous, qualitative, and formative feedback on student programming submissions via GitHub, which can then be validated and enhanced by professionals like eg. tutors of the course. 
It is to relieve the strain on tutors to not have to look at hundreds of lines of code trying to figure out certain mistakes or errors. This is because looking at the same exercises over and over can create fatigue or influence the quality of the review in the long run.
AI can discern certain mistakes or better ways to write some passages in the code, which the still learning tutor may miss or overlook. 

### Validation: 
Success will be measured by 
- the tutors themselves by checking the remarks the AI made and validating them for accuracy and plausability
- and Feedback Utility measured via student surveys regarding the clarity and actionability of the AI comments.

### What system, feature or workflow will you develop or analyze? 
Workflow: 
- Ingestion: Automatically fetch student source code from GitHub.
- Execution: Run a dedicated test suite to capture runtime logs and execution data.
- Analysis: Pass the source code and test logs to an AI agent for synthesis.
- Delivery: Generate a structured "Code Review" (JSON format) pushed via the GitHub API as a comment on the repository.

### How does AI assistance contribute to the development process?
Provides code structure and writes the complex and critical parts of the source code. 
It also is there to provide guidance and recommendations in developing the project structure and use of technologies. 


### Provide development/architecture diagram illustrating the project idea (software components, roles and use of AI tools, interaction between human and AI)



## Project Plan
Main deadline (Project): May 28th, 2026

- Developing code and ensuring that it works as intended
- Implementing a UI for better usage of the application
- Testing usability and correctness of the application and reviewing it


## Teamwork & Responsibilieties
### Egor Podverbnii
Main Backend-Developer for the project, responsible for a clear code structure and the main parts of the code.

### David Görg
Main Frontend-Developer, mainly responsible for the UI of the application and general support regarding documentation

### David Sekot
Main Focus on the documentation and progress of the project as well as supporting the other devs with their main tasks. 
