window.addEventListener("load", function() {
    let gameId;
    let gameState;
    let turn = false;
    let username;
    let firstPlayer;
    let secondPlayer;
    let firstScore = 0;
    let secondScore = 0;
    let playerCheckers;
    let kingCheckers = [];
    let isDoubleJump = false;
    let doubleJumpChecker;
    let selectedCheckerSpan;
    let possibleCaptureTiles = [];
    let capturedCheckers ="";
    let playerMove;
    let opponentMove;
    let winner ="";

   // checkers position
    const game = [
        null, 0, null, 1, null, 2, null, 3, 
        4, null, 5, null, 6, null, 7, null,
        null, 8, null, 9, null, 10, null, 11, 
        null, null, null,null, null, null, null, null, 
        null, null, null, null, null, null, null, null, 
        12, null, 13, null, 14, null, 15, null,
        null, 16, null, 17, null, 18, null, 19, 
        20, null, 21, null, 22, null, 23, null,
    ];
    
    /**
     * Retrieve html elements
     */
    const board = document.querySelector(".board");
    const usernameInput = document.querySelector("#username");
    const registerButton = document.querySelector("#register");
    const tryGameButton = document.querySelector('#try-game');
    const getMoveButton = document.querySelector('#get-move');
    const sendMoveButton = document.querySelector('#send-move');
    const quitGameButton = document.querySelector('#quit-game');

    /**
     * Set up game board and checker pieces
     */
    renderTiles();

    function renderTiles() {
        for( let i = 0; i < 8; i++) {
            // create row
            let tr = document.createElement('tr');
            for(let j = 0; j < 8; j++) {
                // create cell
                let td = document.createElement('td');
                td.setAttribute('id', String.fromCharCode(97 + j) + (i + 1));
                if((i + j) %2 == 0) {
                    // light color for even tiles
                    td.setAttribute('class', 'tile lightTile');
                }else {
                    // dark color for odd tiles
                    td.setAttribute('class', 'tile darkTile');
                    td.addEventListener('click', () => {
                        if(td.classList.contains('highlight')) {
                            handleMoves(td);
                        }
                    })
                }
                tr.appendChild(td);
            }
            board.appendChild(tr);
        }
        renderChecker();
    }
    
    function renderChecker() {
        for(let i = 0; i < game.length; i ++) {
            if(game[i] != null) {
            const tileId = converIndexToTileId(i);
            const tileDiv = document.getElementById(tileId);
            const checkerSpan = document.createElement('span');
            const checker = game[i] > 11 ? "blackChecker" : "redChecker";
            checkerSpan.setAttribute('id', game[i]);
            checkerSpan.setAttribute('class', `checker ${checker}`);
            tileDiv.appendChild(checkerSpan);
            }
        }
    }

    function handleKingTiles() {
        // make checker a king when arrive opponent's last row
        const redLastRow = board.firstChild;
        const blackKingTiles = redLastRow.querySelectorAll(".darkTile");
        const blackLastRow = board.lastChild;
        const redKingTiles = blackLastRow.querySelectorAll(".darkTile");
        const kingIcon = "<i class='fa fa-solid fa-crown'></i>";
        blackKingTiles.forEach(tile => {
            // make black checkers king when arrive these tiles
            const checker = tile.firstChild;
            if(checker) {
                if(checker.classList.contains("blackChecker")) {
                    checker.innerHTML = kingIcon;
                }
                if(playerCheckers == "blackChecker" && !kingCheckers.includes(checker.id)) {
                    kingCheckers.push(checker.id);
                }
            }
           
        })

        redKingTiles.forEach(tile => {
            // make red checkers king when arrive these tiles
            const checker = tile.firstChild;
            if(checker) {
                if(checker.classList.contains("redChecker")) {
                    checker.innerHTML = kingIcon;
                }
                if(playerCheckers == "redChecker" && !kingCheckers.includes(checker.id)){
                    kingCheckers.push(checker.id);
                }
            }
        })
    }
    
    /**
     * Handle checkers click
     */
    // player will only allowed to click on own checker pieces
    function handleCheckersClick() {
        const checkerSpan = document.querySelectorAll(`.${playerCheckers}`);
            checkerSpan.forEach(span => {
                if(turn) {
                    span.classList.add('pointer');
                }
                span.addEventListener('click',() => {
                    if(turn) {
                        removeHighlight();
                        checkerSpan.forEach((checker) => {
                            checker.classList.remove('selected');
                        })
                        selectedCheckerSpan = span;
                        span.classList.add('selected');
                        highlightTiles();
                    }
                })
            })
    }

    function disabledClick() {
        const checkerSpan = document.querySelectorAll(`.${playerCheckers}`);
        checkerSpan.forEach(span=> {
            if(span != doubleJumpChecker) {
                // only double jump checker will be able to move;
                span.classList.remove('pointer');
                span.classList.remove('selected');
                // remove event listener
                span.replaceWith(span.cloneNode(true));
                
            } 
        })
        if(!doubleJumpChecker) {
            isDoubleJump = false;
        }
        doubleJumpChecker = null; 
        
        removeHighlight();
    }

    function highlightTiles() {
        const possibleTiles = calculatePossibleMoves();
        possibleTiles.forEach(tileId=> {
            const tileDiv = document.getElementById(`${tileId}`);
            tileDiv.classList.add('highlight');
            tileDiv.classList.add('pointer');
        })
    }

    function removeHighlight() {
        const tileDiv = document.querySelectorAll('.tile');
        tileDiv.forEach(div=> {
            div.classList.remove('highlight');
            div.classList.remove('pointer');
        })
    }

    function handleMoves(newTileDiv) {
        const oldTileDiv = selectedCheckerSpan.parentNode;
        const newTileId = newTileDiv.id;
        const oldTileId = oldTileDiv.id;
        moveCheckers(selectedCheckerSpan.id, newTileId);
       
        isDoubleJump = false;
        if(possibleCaptureTiles.includes(newTileId)) {
            const capturedTileRow = oldTileId[1] > newTileId[1] ? parseInt(oldTileId[1])- 1 : parseInt(oldTileId[1]) + 1;
            const capturedTileCol = oldTileId[0] > newTileId[0] ? oldTileId[0].charCodeAt(0) - 1 : oldTileId[0].charCodeAt(0) + 1
            const capturedTileId = String.fromCharCode(capturedTileCol) + capturedTileRow;
            const capturedTile = document.getElementById(capturedTileId);
            removeChecker(capturedTile);
            
            possibleCaptureTiles = [];
            isDoubleJump = true;
            calculatePossibleMoves();
            if(possibleCaptureTiles.length != 0) {
                doubleJumpChecker = selectedCheckerSpan;
            }
        }
        playerMove = `${selectedCheckerSpan.id}:${newTileId}`;
        if(kingCheckers.includes(selectedCheckerSpan.id)) {
            playerMove = "*" + playerMove;
        }
        sendMoveButton.disabled = false;
        disabledClick();
    }

    function moveCheckers(checkerId, tileId) {
        let isKing = false;
        let checker = checkerId;
        
        if(checkerId[0] == "*") {
            isKing = true;
            checker = checkerId.substring(1);
        }
        const checkerSpan = document.getElementById(checker);
        const newTileDiv = document.getElementById(tileId);
        const oldTileDiv = checkerSpan.parentNode;

        if(isKing) {
            checkerSpan.innerHTML = "<i class='fa fa-solid fa-crown'></i>";
        }
        const newIndex = convertTileIdToIndex(tileId);
        const oldIndex = convertTileIdToIndex(oldTileDiv.id);
        oldTileDiv.removeChild(checkerSpan);
        newTileDiv.appendChild(checkerSpan);
        game[oldIndex] = null;
        console.log(`Add checker id${game[oldIndex]} to ${newIndex} to move`);
        game[newIndex] = checkerSpan.id;
        console.log(`Set checker id${game[oldIndex]} to null to move`);
        
        handleKingTiles();
    }

    function removeChecker(tileDiv) {
        const checkerSpan = tileDiv.lastElementChild;
        const tileIndex = convertTileIdToIndex(tileDiv.id);
        console.log(`Removed checker id ${game[tileIndex]} from ${tileIndex}`);
        game[tileIndex] = null;
        if(!checkerSpan.classList.contains(playerCheckers)) {
            // captured opponent's checker
            capturedCheckers += `${checkerSpan.id},`;
            console.log(capturedCheckers);
            if(firstPlayer == username) {
                firstScore ++
            } else {
                secondScore ++
            }
        } else {
            // opponet captured player's checker
            if(firstPlayer == username) {
                secondScore ++;
            } else {
                firstScore ++;
            }
        }
        tileDiv.removeChild(checkerSpan);
        updateScoreRecord();
    }

    function calculatePossibleMoves() {
        const selectedTileDiv = selectedCheckerSpan.parentNode;
        const selectedTileCol = parseInt(selectedTileDiv.id[0].charCodeAt(0));
        const selectedTileRow = parseInt(selectedTileDiv.id[1]);
        let possibleMoves = [];
        let possibleRows = [];
        let possibleCols = [];
        if(selectedTileCol > 97) {
            possibleCols.push(selectedTileCol - 1 );
        }
        if(selectedTileCol < 104) {
            possibleCols.push(selectedTileCol + 1);
        }
        if(playerCheckers == "blackChecker" || kingCheckers.includes(selectedCheckerSpan.id)) {
            if(selectedTileRow > 1) {
                possibleRows.push(selectedTileRow - 1);
            }
        } 
        if (playerCheckers == "redChecker" || kingCheckers.includes(selectedCheckerSpan.id)) {
            if(selectedTileRow < 8) {
                possibleRows.push(selectedTileRow + 1);
            }   
        }
        possibleRows.forEach(r => {
            possibleCols.forEach(c => {
                const tileId = String.fromCharCode(c) + r;
                const index = convertTileIdToIndex(tileId);
                if(game[index] == null) {
                    if(!isDoubleJump) {
                        // if it's double jump, must capture checker to move
                        possibleMoves.push(tileId);
                    } 
                } else {
                    const blockedCheckerSpan = document.getElementById(`${game[index]}`);
                    const opponentCheckers = playerCheckers == "redChecker" ? "blackChecker" : "redChecker";
                    if(blockedCheckerSpan.classList.contains(opponentCheckers)) {                     
                        const jumpTileRow = selectedTileRow > r ? r - 1 : r + 1; 
                        const jumpTileCol = selectedTileCol > c ? c - 1 : c + 1;
                        if(jumpTileRow > 0 && jumpTileRow <= 8 && jumpTileCol >= 97 && jumpTileCol <= 104) {
                            const jumpTileId = String.fromCharCode(jumpTileCol) + jumpTileRow;
                            const jumpTileIndex = convertTileIdToIndex(jumpTileId);
                            if(game[jumpTileIndex] == null) {
                                possibleMoves.push(jumpTileId);
                                possibleCaptureTiles.push(jumpTileId);
                            }
                        }
                    }
                }
            })
        })
        return possibleMoves;
    }
   
    function convertTileIdToIndex(tile) {
        const col = tile[0].charCodeAt(0);
        const row = tile[1];
        return (col - 97) + ((row -1) * 8)
    }

    function converIndexToTileId(index) {
        const tileRow = Math.floor((index / 8)) + 1;
        const tileCol = String.fromCharCode((index % 8) + 97);
        return tileCol + tileRow;
    }

    /**
     * Settings event listeners
     */
    registerButton.addEventListener("click", registerUsername);
    tryGameButton.addEventListener("click", pairme);
    sendMoveButton.addEventListener("click", sendMove);
    getMoveButton.addEventListener("click", getMove);
    quitGameButton.addEventListener("click",quitGame);
    
    /**
     * Fetch data from server
     */
    async function registerUsername() {
        fetch('http://localhost:8080/register', {
            method: "GET"
        })
        .then(response => {
            return response.json();
        })
        .then(content => {
            username = content;
            displayUsername();
            quitGameButton.disabled = false;
        })
        .catch(error => {
          console.error('An error occurred:', error);
        });
    }
  
    async function pairme() {
        username = usernameInput.value;
        fetch(`http://localhost:8080/pairme?player=${username}`, {
            method: "GET"
        })
        .then(response => {
            return response.json();
        })
        .then(content => {
            handleGameState(content);
        })
        .catch(error => {
          console.error('An error occurred:', error);
        });
    }

    async function sendMove() {
        turn = false;
        possibleCaptureTiles = [];
        fetch(`http://localhost:8080/mymove?player=${username}&id=${gameId}&move=${playerMove}-${capturedCheckers}`, {
            method: "GET"
        })
        .then(response => {
            return response.text();
        })
        .then(content => {
            handleSendMove(content);
        })
        .catch(error => {
          console.error('An error occurred:', error);
        });
    }

    async function getMove() {
        fetch(`http://localhost:8080/theirmove?player=${username}&id=${gameId}`, {
            method: "GET"
        })
        .then(response => {
            return response.text();
        })
        .then(content => {
            handleGetMove(content);
        })
        .catch(error => {
          console.error('An error occurred:', error);
        });
    }

    async function quitGame() {
        fetch(`http://localhost:8080/quit?player=${username}&id=${gameId}`, {
            method: "GET"
        })
        .then(response => {
            return response.text();
        })
        .then(content => {
            if(content == "OK"){
                updateStatus("Game over! Refresh to play again.");
            } else {
                updateStatus("Error! Try again.");
            }
        })
        .catch(error => {
          console.error('An error occurred:', error);
        });
        registerButton.disabled = true;
            tryGameButton.disabled = true;
            sendMoveButton.disabled = true;
            getMoveButton.disabled = true;
            quitGameButton.disabled = true;
    }

    function handleGameState(content) {
        if(content == "Not OK") {
            updateStatus("Error, refresh to try again.");
            return
        }
        usernameInput.disabled = true;
        registerButton.disabled = true;
        gameId = content.GameId;
        gameState = content.GameState;
        firstPlayer = content.FirstPlayer;
        secondPlayer = content.SecondPlayer;
        updatePlayerRecord();
        if(gameState == "progress") {
            startGame();
        } else {
            updateStatus("No player at the moment. Try again.")
        }
    }

    function startGame() {
        tryGameButton.disabled = true;
        if(firstPlayer == username) {
            // player play first
            turn = true;
            updateStatus(`Game Start! Your turn. Click "Send my move" when done.`)
            playerCheckers = "redChecker";
        } else {
            updateStatus(`Game Start! Opponent's turn. Click "Get their move".`);
            getMoveButton.disabled = false;
            playerCheckers = "blackChecker";
        }
        updateScoreRecord();
        handleCheckersClick();
    }

    function handleSendMove(content) {
        if(content == "OK") {
            if(!winner){
                updateStatus(`Opponent's turn. Click "Get their move".`);
                capturedCheckers = "";
            }
            sendMoveButton.disabled = true;
            getMoveButton.disabled = false;
        } else {
            updateStatus(`Disconnected. Refresh to play.`);
        }
    }

    function handleGetMove(content) {
        if(content == "Not OK"){
            updateStatus(`Disconnected. Refresh to play.`);
            return;
        }
        if(content == "null" || content == opponentMove) {
            updateStatus("Waiting for opponent to make their move. Try again.");
        } else {
            opponentMove = content;
            if(!winner) {
                updateStatus(`Your turn. Click "Send my move" when done.`);
            }
            getMoveButton.disabled = true;
            const dashIndex = content.indexOf('-');
            const checkerTile = content.substring(1,dashIndex);
            const colonIndex = checkerTile.indexOf(':');
            const movedCheckerId = checkerTile.substring(0, colonIndex);
            const movedTileId = checkerTile.substring(colonIndex + 1);
            const capturedCheckers = content.substring(dashIndex + 1, content.length-1);
            
            if(capturedCheckers != "") {
                const capturedCheckerArr = capturedCheckers.split(',');
                for(let i = 0; i < capturedCheckerArr.length; i++) {
                    const capturedCheckerSpan = document.getElementById(`${capturedCheckerArr[i]}`);
                    if(capturedCheckerSpan) {
                        removeChecker(capturedCheckerSpan.parentNode);
                    }
                }
            }
        moveCheckers(movedCheckerId, movedTileId)
        turn = true;
            handleCheckersClick();  
        }
    }
    /**
     * Display information
     */
    function displayUsername() { 
        usernameInput.value = username
        updateStatus("To start, click 'Try Game'.");
    }
    
    function updateStatus(content) {
        const statusDiv = document.getElementById('status');
        statusDiv.innerHTML = `<p>${content}<p>`;
    }

    function updatePlayerRecord() {
        const firstUsername = document.getElementById("username1");
        const secondUsername = document.getElementById("username2");
        firstUsername.innerHTML = firstPlayer;
        if(secondPlayer != null) {
            secondUsername.innerHTML = secondPlayer;   
        }
    }

    function updateScoreRecord() {
        const firstScoreRecord = document.getElementById("score1");
        const secondScoreRecord = document.getElementById("score2");
        firstScoreRecord.innerHTML = firstScore + "/12";
        secondScoreRecord.innerHTML = secondScore + "/12";
        if(firstScore  == 12 || secondScore == 12) {
            winner = firstScore == 12 ? firstPlayer : secondPlayer;
            if(winner == username) {
                updateStatus("You win!");
            } else {
                quitGame();
                updateStatus("You lose!");
            }
        }
    }
    
})
