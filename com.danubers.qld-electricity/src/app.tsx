import * as React from 'react';
import * as ReactDOM from 'react-dom';
import {Provider} from "react-redux";
import reducer from './store'
import DateRefresher from './components/dateRefresherComponent'
import {createStore} from "redux";

const store = createStore(reducer);

ReactDOM.render(
    <Provider store={store}>
        <DateRefresher></DateRefresher>
    </Provider>,
    document.getElementById("root")
)